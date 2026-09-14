using Inventory.Application.Common;

namespace Inventory.Application.Categories.Queries.GetCategories;

public sealed class GetCategoriesHandler
{
    private readonly ICategoryReadStore _store;

    public GetCategoriesHandler(ICategoryReadStore store)
    {
        _store = store;
    }

    public Task<PagedResult<CategoryDto>> HandleAsync(
        GetCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        return _store.GetPagedAsync(
            query.Page,
            query.PageSize,
            cancellationToken);
    }
}
