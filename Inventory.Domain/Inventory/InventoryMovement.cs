namespace Inventory.Domain.Inventory;

public sealed class InventoryMovement
{
    public long Id { get; set; }
    public Guid ProductId { get; set; }
    public InventoryMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal StockAfterMovement { get; set; }
    public required string IdempotencyKey { get; set; }
    public required string RequestFingerprint { get; set; }
    public DateTime CreatedAt { get; set; }
}
