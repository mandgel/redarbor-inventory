namespace Inventory.Api.Categories;

public sealed record UpdateCategoryRequest
{
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
}
