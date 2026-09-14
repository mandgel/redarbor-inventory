namespace Inventory.Application.Inventory.Commands.RegisterInventoryMovement;

public sealed class InsufficientStockException : Exception
{
    public InsufficientStockException()
        : base("Insufficient stock to complete the inventory movement.")
    {
    }
}