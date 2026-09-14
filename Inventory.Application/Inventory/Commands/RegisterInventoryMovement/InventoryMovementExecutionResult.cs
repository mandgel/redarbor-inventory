namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public sealed record InventoryMovementExecutionResult
{
    public required InventoryMovementExecutionStatus Status { get; init; }
    public RegisterInventoryMovementResult? Result { get; init; }
}
