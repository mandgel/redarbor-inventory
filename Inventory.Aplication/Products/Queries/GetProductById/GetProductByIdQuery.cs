namespace Inventory.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery
{
    public required Guid Id { get; init; }
}
