namespace Inventory.Infrastructure.Persistence.Commands;

internal sealed record InsertedProduct
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Sku { get; init; }
    public required Guid CategoryId { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
