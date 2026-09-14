namespace Inventory.Application.Categories.Queries.GetCategoryById;

public sealed class GetCategoryByIdHandler
{
    private readonly ICategoryReadStore _store;

    public GetCategoryByIdHandler(ICategoryReadStore store)
    {
        _store = store;
    }

    public Task<CategoryDto?> HandleAsync(
        GetCategoryByIdQuery query,
        CancellationToken cancellationToken)
    {
        return _store.GetByIdAsync(
            query.Id,
            cancellationToken);
    }
}
