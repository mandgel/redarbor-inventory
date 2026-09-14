using Dapper;
using Inventory.Application.Inventory.Commands.RegisterInventoryMovement;
using Inventory.Domain.Inventory;
using Inventory.Infrastructure.Persistence.Commands;
using Microsoft.Data.SqlClient;

namespace Inventory.IntegrationTests.Inventory;

public sealed class DapperInventoryMovementStoreTests
{
    
    [Fact]
    public async Task ExecuteAsync_WhenOutboundMovementIsValid_ShouldPersistMovementAndUpdateBalance()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        await SeedProductAsync(
            categoryId,
            productId,
            initialStock: 10m);

        var store = new DapperInventoryMovementStore(
            GetConnectionString());

        var command = new RegisterInventoryMovementCommand
        {
            ProductId = productId,
            MovementType = InventoryMovementType.Outbound,
            Quantity = 4m,
            IdempotencyKey = $"integration-{Guid.NewGuid()}"
        };

        var execution = new InventoryMovementExecution
        {
            Command = command,
            BalanceChange = new InventoryBalanceChange
            {
                ProductId = productId,
                QuantityChange = -4m,
                PreventNegativeStock = true
            },
            RequestFingerprint =
                InventoryMovementFingerprint.Create(command)
        };

        var result = await store.ExecuteAsync(
            execution,
            CancellationToken.None);

        Assert.Equal(
            InventoryMovementExecutionStatus.Applied,
            result.Status);

        Assert.NotNull(result.Result);
        Assert.Equal(6m, result.Result.CurrentStock);

        await AssertDatabaseStateAsync(
            productId,
            command.IdempotencyKey,
            expectedStock: 6m);
    }

    private static async Task SeedProductAsync(
    Guid categoryId,
    Guid productId,
    decimal initialStock)
    {
        await using var connection =
            new SqlConnection(GetConnectionString());

        await connection.OpenAsync();

        const string sql = """
        INSERT INTO Categories
        (
            Id,
            Name,
            IsActive,
            IsDeleted,
            CreatedAt,
            UpdatedAt
        )
        VALUES
        (
            @CategoryId,
            'Integration Category',
            1,
            0,
            SYSUTCDATETIME(),
            SYSUTCDATETIME()
        );

        INSERT INTO Products
        (
            Id,
            Name,
            Description,
            Sku,
            CategoryId,
            IsActive,
            IsDeleted,
            CreatedAt,
            UpdatedAt
        )
        VALUES
        (
            @ProductId,
            'Integration Product',
            NULL,
            @Sku,
            @CategoryId,
            1,
            0,
            SYSUTCDATETIME(),
            SYSUTCDATETIME()
        );

        INSERT INTO InventoryBalances
        (
            ProductId,
            CurrentStock,
            UpdatedAt
        )
        VALUES
        (
            @ProductId,
            @InitialStock,
            SYSUTCDATETIME()
        );
        """;

        await connection.ExecuteAsync(
            sql,
            new
            {
                CategoryId = categoryId,
                ProductId = productId,
                Sku = $"IT-{Guid.NewGuid():N}"[..20],
                InitialStock = initialStock
            });
    }

    private static async Task AssertDatabaseStateAsync(
    Guid productId,
    string idempotencyKey,
    decimal expectedStock)
    {
        await using var connection =
            new SqlConnection(GetConnectionString());

        await connection.OpenAsync();

        var currentStock =
            await connection.QuerySingleAsync<decimal>(
                """
            SELECT CurrentStock
            FROM InventoryBalances
            WHERE ProductId = @ProductId;
            """,
                new { ProductId = productId });

        var movementCount =
            await connection.QuerySingleAsync<int>(
                """
            SELECT COUNT(*)
            FROM InventoryMovements
            WHERE IdempotencyKey = @IdempotencyKey;
            """,
                new { IdempotencyKey = idempotencyKey });

        Assert.Equal(expectedStock, currentStock);
        Assert.Equal(1, movementCount);

    }

    private static string GetConnectionString()
    {
        return Environment.GetEnvironmentVariable(
            "ConnectionStrings__InventoryDatabase")
            ?? throw new InvalidOperationException(
                "ConnectionStrings__InventoryDatabase is not configured.");
    }
}