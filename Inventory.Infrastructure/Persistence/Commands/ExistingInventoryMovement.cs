namespace Inventory.Infrastructure.Persistence.Commands;

internal sealed record ExistingInventoryMovement
{
    public required long Id { get; init; }
    public required Guid ProductId { get; init; }
    public required byte MovementType { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal StockAfterMovement { get; init; }
    public required string RequestFingerprint { get; init; }
    public required DateTime CreatedAt { get; init; }
}
