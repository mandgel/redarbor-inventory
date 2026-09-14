using Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

namespace Inventory.UnitTests.Inventory;

internal sealed class FakeInventoryMovementStore : IInventoryMovementStore
{
    private readonly Dictionary<string, IdempotencyEntry> _idempotencyEntries = [];
    private long _nextMovementId = 1;

    public decimal CurrentStock { get; private set; }

    public FakeInventoryMovementStore(decimal currentStock)
    {
        CurrentStock = currentStock;
    }

    public Task<InventoryMovementExecutionResult> ExecuteAsync(
        InventoryMovementExecution execution,
        CancellationToken cancellationToken)
    {
        var idempotencyResult = TryGetIdempotencyResult(execution);

        if (idempotencyResult is not null)
        {
            return Task.FromResult(idempotencyResult);
        }

        return Task.FromResult(ApplyMovement(execution));
    }

    public void SetCurrentStock(decimal currentStock)
    {
        CurrentStock = currentStock;
    }

    private InventoryMovementExecutionResult? TryGetIdempotencyResult(
        InventoryMovementExecution execution)
    {
        var idempotencyKey = execution.Command.IdempotencyKey;

        if (!_idempotencyEntries.TryGetValue(idempotencyKey, out var entry))
        {
            return null;
        }

        if (entry.Fingerprint != execution.RequestFingerprint)
        {
            return new InventoryMovementExecutionResult
            {
                Status = InventoryMovementExecutionStatus.IdempotencyConflict
            };
        }

        return new InventoryMovementExecutionResult
        {
            Status = InventoryMovementExecutionStatus.Replayed,
            Result = entry.Result
        };
    }

    private InventoryMovementExecutionResult ApplyMovement(
        InventoryMovementExecution execution)
    {
        var balanceChange = execution.BalanceChange;
        var resultingStock = CurrentStock + balanceChange.QuantityChange;

        if (balanceChange.PreventNegativeStock && resultingStock < 0)
        {
            return new InventoryMovementExecutionResult
            {
                Status = InventoryMovementExecutionStatus.InsufficientStock
            };
        }

        CurrentStock = resultingStock;

        var result = CreateResult(execution.Command);

        _idempotencyEntries[execution.Command.IdempotencyKey] = new IdempotencyEntry(
            execution.RequestFingerprint,
            result);

        return new InventoryMovementExecutionResult
        {
            Status = InventoryMovementExecutionStatus.Applied,
            Result = result
        };
    }

    private RegisterInventoryMovementResult CreateResult(
        RegisterInventoryMovementCommand command)
    {
        return new RegisterInventoryMovementResult
        {
            MovementId = _nextMovementId++,
            ProductId = command.ProductId,
            MovementType = command.MovementType,
            Quantity = command.Quantity,
            CurrentStock = CurrentStock,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private sealed record IdempotencyEntry(
        string Fingerprint,
        RegisterInventoryMovementResult Result);
}
