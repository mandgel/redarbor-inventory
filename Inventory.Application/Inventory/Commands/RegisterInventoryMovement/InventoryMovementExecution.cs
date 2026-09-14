namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public sealed record InventoryMovementExecution
{
    public required RegisterInventoryMovementCommand Command { get; init; }
    public required InventoryBalanceChange BalanceChange { get; init; }
    public required string RequestFingerprint { get; init; }
}
