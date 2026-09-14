using Inventory.Domain.Categories;

namespace Inventory.Application.Categories;

public sealed record CategoryDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }

    public static CategoryDto FromEntity(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}
