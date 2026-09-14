using Inventory.Domain.Inventory;

namespace Inventory.Api.InventoryMovements;

public sealed record RegisterInventoryMovementRequest
{
    public required Guid ProductId { get; init; }
    public required InventoryMovementType MovementType { get; init; }
    public required decimal Quantity { get; init; }
}
