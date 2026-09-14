namespace Inventory.Application.Categories.Queries.GetCategories;

public sealed record GetCategoriesQuery
{
    public required int Page { get; init; }
    public required int PageSize { get; init; }
}
