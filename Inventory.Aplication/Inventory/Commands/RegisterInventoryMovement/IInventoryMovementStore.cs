namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public interface IInventoryMovementStore
{
    Task<InventoryMovementExecutionResult> ExecuteAsync(
        InventoryMovementExecution execution,
        CancellationToken cancellationToken);
}
