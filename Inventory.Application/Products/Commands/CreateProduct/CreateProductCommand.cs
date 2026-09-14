namespace Inventory.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Sku { get; init; }
    public required Guid CategoryId { get; init; }
    public required bool IsActive { get; init; }
}
