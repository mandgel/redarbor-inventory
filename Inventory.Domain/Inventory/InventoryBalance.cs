namespace Inventory.Domain.Inventory;

public sealed class InventoryBalance
{
    public Guid ProductId { get; set; }
    public decimal CurrentStock { get; set; }
    public DateTime UpdatedAt { get; set; }
}
