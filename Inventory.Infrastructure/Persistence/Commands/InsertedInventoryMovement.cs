namespace Inventory.Infrastructure.Persistence.Commands;

internal sealed record InsertedInventoryMovement
{
    public required long Id { get; init; }
    public required DateTime CreatedAt { get; init; }
}
