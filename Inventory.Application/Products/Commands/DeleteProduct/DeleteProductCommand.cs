namespace Inventory.Application.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand
{
    public required Guid Id { get; init; }
}
