namespace Inventory.Application.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
}
