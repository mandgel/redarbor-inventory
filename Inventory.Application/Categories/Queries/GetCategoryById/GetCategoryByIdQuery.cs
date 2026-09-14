namespace Inventory.Application.Categories.Queries.GetCategoryById;

public sealed record GetCategoryByIdQuery
{
    public required Guid Id { get; init; }
}
