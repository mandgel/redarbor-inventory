using Inventory.Domain.Inventory;

namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public sealed class RegisterInventoryMovementHandler
{
    private readonly IInventoryBalanceStore _store;
    private readonly NegativeStockPolicy _negativeStockPolicy;
    private readonly IIdempotencyStore _idempotencyStore;

    public RegisterInventoryMovementHandler(
        IInventoryBalanceStore store,
        IIdempotencyStore idempotencyStore,
        NegativeStockPolicy negativeStockPolicy)
    {
        _store = store;
        _idempotencyStore = idempotencyStore;
        _negativeStockPolicy = negativeStockPolicy;
    }

    public async Task<RegisterInventoryMovementResult> HandleAsync(
    RegisterInventoryMovementCommand command,
    CancellationToken cancellationToken)
    {
        var fingerprint = InventoryMovementFingerprint.Create(command);

        var previousResult = await GetPreviousResultAsync(
            command,
            fingerprint,
            cancellationToken);

        if (previousResult is not null)
        {
            return previousResult;
        }

        var result = await ApplyMovementAsync(
            command,
            cancellationToken);

        await SaveIdempotencyRecordAsync(
            command,
            fingerprint,
            result,
            cancellationToken);

        return result;
    }

    private InventoryBalanceChange CreateBalanceChange(
        RegisterInventoryMovementCommand command)
    {
        return new InventoryBalanceChange
        {
            ProductId = command.ProductId,
            QuantityChange = GetQuantityChange(command),
            PreventNegativeStock =
                _negativeStockPolicy == NegativeStockPolicy.Reject
        };
    }

    private static decimal GetQuantityChange(
        RegisterInventoryMovementCommand command)
    {
        return command.MovementType switch
        {
            InventoryMovementType.Inbound => command.Quantity,
            InventoryMovementType.Outbound => -command.Quantity,
            _ => throw new ArgumentOutOfRangeException(
                nameof(command.MovementType))
        };
    }

    private static RegisterInventoryMovementResult CreateResult(
        RegisterInventoryMovementCommand command,
        decimal currentStock)
    {
        return new RegisterInventoryMovementResult
        {
            MovementId = 1,
            ProductId = command.ProductId,
            MovementType = command.MovementType,
            Quantity = command.Quantity,
            CurrentStock = currentStock,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task<RegisterInventoryMovementResult?> GetPreviousResultAsync(
    RegisterInventoryMovementCommand command,
    string fingerprint,
    CancellationToken cancellationToken)
    {
        var previousRecord = await _idempotencyStore.GetAsync(
            command.IdempotencyKey,
            cancellationToken);

        if (previousRecord is null)
        {
            return null;
        }

        if (previousRecord.RequestFingerprint != fingerprint)
        {
            throw new IdempotencyConflictException();
        }

        return previousRecord.Result;
    }

    private async Task<RegisterInventoryMovementResult> ApplyMovementAsync(
    RegisterInventoryMovementCommand command,
    CancellationToken cancellationToken)
    {
        var change = CreateBalanceChange(command);

        var balanceResult = await _store.ApplyMovementAsync(
            change,
            cancellationToken);

        if (!balanceResult.Applied)
        {
            throw new InsufficientStockException();
        }

        return CreateResult(
            command,
            balanceResult.CurrentStock!.Value);
    }
    private Task SaveIdempotencyRecordAsync(
    RegisterInventoryMovementCommand command,
    string fingerprint,
    RegisterInventoryMovementResult result,
    CancellationToken cancellationToken)
    {
        var record = new IdempotencyRecord
        {
            IdempotencyKey = command.IdempotencyKey,
            RequestFingerprint = fingerprint,
            Result = result
        };

        return _idempotencyStore.SaveAsync(
            record,
            cancellationToken);
    }
}