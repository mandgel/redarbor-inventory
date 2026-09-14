namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public enum InventoryMovementExecutionStatus
{
    Applied = 1,
    Replayed = 2,
    InsufficientStock = 3,
    IdempotencyConflict = 4,
    ProductNotFound = 5
}
