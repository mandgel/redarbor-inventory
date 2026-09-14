using Inventory.Domain.Inventory;

namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public sealed class RegisterInventoryMovementHandler
{
    private readonly IInventoryMovementStore _store;
    private readonly NegativeStockPolicy _negativeStockPolicy;

    public RegisterInventoryMovementHandler(
        IInventoryMovementStore store,
        NegativeStockPolicy negativeStockPolicy)
    {
        _store = store;
        _negativeStockPolicy = negativeStockPolicy;
    }

    public async Task<RegisterInventoryMovementResult> HandleAsync(
        RegisterInventoryMovementCommand command,
        CancellationToken cancellationToken)
    {
        var execution = CreateExecution(command);

        var executionResult = await _store.ExecuteAsync(
            execution,
            cancellationToken);

        return MapResult(executionResult);
    }

    private InventoryMovementExecution CreateExecution(
        RegisterInventoryMovementCommand command)
    {
        var fingerprint = InventoryMovementFingerprint.Create(command);

        var balanceChange = CreateBalanceChange(command);

        return new InventoryMovementExecution
        {
            Command = command,
            BalanceChange = balanceChange,
            RequestFingerprint = fingerprint
        };
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

    private static RegisterInventoryMovementResult MapResult(
        InventoryMovementExecutionResult executionResult)
    {
        return executionResult.Status switch
        {
            InventoryMovementExecutionStatus.Applied => executionResult.Result!,
            InventoryMovementExecutionStatus.Replayed => executionResult.Result!,
            InventoryMovementExecutionStatus.InsufficientStock =>
                throw new InsufficientStockException(),
            InventoryMovementExecutionStatus.IdempotencyConflict =>
                throw new IdempotencyConflictException(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(executionResult.Status))
        };
    }
}