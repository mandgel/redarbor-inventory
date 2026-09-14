namespace Inventory.Application.Products;

public sealed record ProductWriteData
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Sku { get; init; }
    public required Guid CategoryId { get; init; }
    public required bool IsActive { get; init; }
}
