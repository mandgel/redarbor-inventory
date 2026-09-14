namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetAsync(
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task SaveAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken);
}