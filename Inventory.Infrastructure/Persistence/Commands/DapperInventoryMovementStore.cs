using Dapper;
using Inventory.Application.Inventory.Commands.RegisterInventoryMovement;
using Inventory.Domain.Inventory;
using Microsoft.Data.SqlClient;

namespace Inventory.Infrastructure.Persistence.Commands;

public sealed class DapperInventoryMovementStore : IInventoryMovementStore
{
    private readonly string _connectionString;

    public DapperInventoryMovementStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<InventoryMovementExecutionResult> ExecuteAsync(
        InventoryMovementExecution execution,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var result = await ExecuteWithinTransactionAsync(
                connection,
                transaction,
                execution,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch (SqlException exception)
            when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);

            return await ResolveConcurrentIdempotencyAsync(
                connection,
                execution,
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<InventoryMovementExecutionResult>
        ExecuteWithinTransactionAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            InventoryMovementExecution execution,
            CancellationToken cancellationToken)
    {
        var existingMovement = await GetExistingMovementAsync(
            connection,
            transaction,
            execution.Command.IdempotencyKey,
            cancellationToken);

        if (existingMovement is not null)
        {
            return ResolveExistingMovement(
                existingMovement,
                execution.RequestFingerprint);
        }

        var currentStock = await ApplyBalanceChangeAsync(
            connection,
            transaction,
            execution,
            cancellationToken);

        if (currentStock is null)
        {
            return await ResolveFailedBalanceChangeAsync(
                connection,
                transaction,
                execution,
                cancellationToken);
        }

        var result = await InsertMovementAsync(
            connection,
            transaction,
            execution,
            currentStock.Value,
            cancellationToken);

        return new InventoryMovementExecutionResult
        {
            Status = InventoryMovementExecutionStatus.Applied,
            Result = result
        };
    }

    private static async Task<ExistingInventoryMovement?>
        GetExistingMovementAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP 1
                Id,
                ProductId,
                MovementType,
                Quantity,
                StockAfterMovement,
                RequestFingerprint,
                CreatedAt
            FROM InventoryMovements
            WHERE IdempotencyKey = @IdempotencyKey;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                IdempotencyKey = idempotencyKey
            },
            transaction,
            cancellationToken: cancellationToken);

        return await connection
            .QuerySingleOrDefaultAsync<ExistingInventoryMovement>(
                command);
    }

    private static async Task<decimal?> ApplyBalanceChangeAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        InventoryMovementExecution execution,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE InventoryBalances
            SET
                CurrentStock = CurrentStock + @QuantityChange,
                UpdatedAt = @UpdatedAt
            OUTPUT INSERTED.CurrentStock
            WHERE ProductId = @ProductId
              AND (
                  @PreventNegativeStock = 0
                  OR CurrentStock + @QuantityChange >= 0
              );
            """;

        var parameters = new
        {
            execution.BalanceChange.ProductId,
            execution.BalanceChange.QuantityChange,
            execution.BalanceChange.PreventNegativeStock,
            UpdatedAt = DateTime.UtcNow
        };

        var command = new CommandDefinition(
            sql,
            parameters,
            transaction,
            cancellationToken: cancellationToken);

        return await connection
            .QuerySingleOrDefaultAsync<decimal?>(
                command);
    }

    private static async Task<InventoryMovementExecutionResult>
        ResolveFailedBalanceChangeAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            InventoryMovementExecution execution,
            CancellationToken cancellationToken)
    {
        var productExists = await ProductExistsAsync(
            connection,
            transaction,
            execution.BalanceChange.ProductId,
            cancellationToken);

        var status = productExists
            ? InventoryMovementExecutionStatus.InsufficientStock
            : InventoryMovementExecutionStatus.ProductNotFound;

        return new InventoryMovementExecutionResult
        {
            Status = status
        };
    }

    private static async Task<bool> ProductExistsAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid productId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT 1
            FROM Products
            WHERE Id = @ProductId
              AND IsDeleted = 0;
            """;

        var command = new CommandDefinition(
            sql,
            new { ProductId = productId },
            transaction,
            cancellationToken: cancellationToken);

        var exists = await connection
            .QuerySingleOrDefaultAsync<int?>(command);

        return exists is not null;
    }

    private static async Task<RegisterInventoryMovementResult>
        InsertMovementAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            InventoryMovementExecution execution,
            decimal currentStock,
            CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO InventoryMovements
            (
                ProductId,
                MovementType,
                Quantity,
                StockAfterMovement,
                IdempotencyKey,
                RequestFingerprint,
                CreatedAt
            )
            OUTPUT
                INSERTED.Id,
                INSERTED.CreatedAt
            VALUES
            (
                @ProductId,
                @MovementType,
                @Quantity,
                @StockAfterMovement,
                @IdempotencyKey,
                @RequestFingerprint,
                @CreatedAt
            );
            """;

        var parameters = BuildInsertMovementParameters(
            execution,
            currentStock);

        var command = new CommandDefinition(
            sql,
            parameters,
            transaction,
            cancellationToken: cancellationToken);

        var inserted = await connection
            .QuerySingleAsync<InsertedInventoryMovement>(
                command);

        return CreateResult(
            execution,
            currentStock,
            inserted);
    }

    private static object BuildInsertMovementParameters(
        InventoryMovementExecution execution,
        decimal currentStock)
    {
        return new
        {
            execution.Command.ProductId,
            MovementType = (byte)execution.Command.MovementType,
            execution.Command.Quantity,
            StockAfterMovement = currentStock,
            execution.Command.IdempotencyKey,
            execution.RequestFingerprint,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static InventoryMovementExecutionResult
        ResolveExistingMovement(
            ExistingInventoryMovement movement,
            string requestFingerprint)
    {
        if (movement.RequestFingerprint != requestFingerprint)
        {
            return new InventoryMovementExecutionResult
            {
                Status =
                    InventoryMovementExecutionStatus.IdempotencyConflict
            };
        }

        return new InventoryMovementExecutionResult
        {
            Status = InventoryMovementExecutionStatus.Replayed,
            Result = CreateReplayResult(movement)
        };
    }

    private static RegisterInventoryMovementResult CreateReplayResult(
        ExistingInventoryMovement movement)
    {
        return new RegisterInventoryMovementResult
        {
            MovementId = movement.Id,
            ProductId = movement.ProductId,
            MovementType =
                (InventoryMovementType)movement.MovementType,
            Quantity = movement.Quantity,
            CurrentStock = movement.StockAfterMovement,
            CreatedAt = ToUtcDateTimeOffset(
                movement.CreatedAt)
        };
    }

    private static RegisterInventoryMovementResult CreateResult(
        InventoryMovementExecution execution,
        decimal currentStock,
        InsertedInventoryMovement inserted)
    {
        return new RegisterInventoryMovementResult
        {
            MovementId = inserted.Id,
            ProductId = execution.Command.ProductId,
            MovementType = execution.Command.MovementType,
            Quantity = execution.Command.Quantity,
            CurrentStock = currentStock,
            CreatedAt = ToUtcDateTimeOffset(
                inserted.CreatedAt)
        };
    }

    private static async Task<InventoryMovementExecutionResult>
        ResolveConcurrentIdempotencyAsync(
            SqlConnection connection,
            InventoryMovementExecution execution,
            CancellationToken cancellationToken)
    {
        var movement =
            await GetExistingMovementAfterRollbackAsync(
                connection,
                execution.Command.IdempotencyKey,
                cancellationToken);

        if (movement is null)
        {
            throw new InvalidOperationException(
                "The idempotency conflict could not be resolved.");
        }

        return ResolveExistingMovement(
            movement,
            execution.RequestFingerprint);
    }

    private static async Task<ExistingInventoryMovement?>
        GetExistingMovementAfterRollbackAsync(
            SqlConnection connection,
            string idempotencyKey,
            CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP 1
                Id,
                ProductId,
                MovementType,
                Quantity,
                StockAfterMovement,
                RequestFingerprint,
                CreatedAt
            FROM InventoryMovements
            WHERE IdempotencyKey = @IdempotencyKey;
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                IdempotencyKey = idempotencyKey
            },
            cancellationToken: cancellationToken);

        return await connection
            .QuerySingleOrDefaultAsync<ExistingInventoryMovement>(
                command);
    }

    private static bool IsUniqueConstraintViolation(
        SqlException exception)
    {
        return exception.Number is 2601 or 2627;
    }

    private static DateTimeOffset ToUtcDateTimeOffset(
        DateTime value)
    {
        return new DateTimeOffset(
            DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc));
    }
}