namespace Inventory.Application.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand
{
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
}
