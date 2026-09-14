using Inventory.Application.Common;

namespace Inventory.Application.Products;

public interface IProductReadStore
{
    Task<ProductDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<PagedResult<ProductDto>> GetPagedAsync(
        ProductQueryFilter filter,
        CancellationToken cancellationToken);
}
