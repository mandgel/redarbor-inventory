namespace Inventory.Application.Products.Queries.GetProducts;

public sealed record GetProductsQuery
{
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public string? Search { get; init; }
    public string? Sku { get; init; }
    public Guid? CategoryId { get; init; }
    public bool? IsActive { get; init; }
}
