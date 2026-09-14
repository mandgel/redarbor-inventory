using Inventory.Application.Common;

namespace Inventory.Application.Categories;

public interface ICategoryReadStore
{
    Task<CategoryDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<PagedResult<CategoryDto>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
