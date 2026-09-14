namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public sealed class ProductNotFoundException : Exception
{
    public ProductNotFoundException()
        : base("The specified product does not exist.")
    {
    }
}
