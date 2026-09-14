namespace Inventory.Api.Categories;

public sealed record CreateCategoryRequest
{
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
}
