using Inventory.Application.Common;

namespace Inventory.Application.Products.Queries.GetProducts;

public sealed class GetProductsHandler
{
    private readonly IProductReadStore _store;

    public GetProductsHandler(IProductReadStore store)
    {
        _store = store;
    }

    public Task<PagedResult<ProductDto>> HandleAsync(
        GetProductsQuery query,
        CancellationToken cancellationToken)
    {
        var filter = new ProductQueryFilter
        {
            Page = query.Page,
            PageSize = query.PageSize,
            Search = query.Search,
            Sku = query.Sku,
            CategoryId = query.CategoryId,
            IsActive = query.IsActive
        };

        return _store.GetPagedAsync(filter, cancellationToken);
    }
}
