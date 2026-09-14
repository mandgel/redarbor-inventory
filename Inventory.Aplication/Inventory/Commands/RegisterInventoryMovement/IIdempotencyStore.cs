namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public interface IIdempotencyStore
{
    Task<RegisterInventoryMovementResult?> GetAsync(
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task SaveAsync(
        string idempotencyKey,
        RegisterInventoryMovementResult result,
        CancellationToken cancellationToken);
}