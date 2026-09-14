namespace Inventory.Application.Categories.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand
{
    public required Guid Id { get; init; }
}
