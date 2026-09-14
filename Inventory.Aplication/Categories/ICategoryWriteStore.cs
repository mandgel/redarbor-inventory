namespace Inventory.Application.Categories;

public interface ICategoryWriteStore
{
    Task<CategoryDto> CreateAsync(
        string name,
        bool isActive,
        CancellationToken cancellationToken);

    Task<CategoryDto?> UpdateAsync(
        Guid id,
        string name,
        bool isActive,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken);
}
