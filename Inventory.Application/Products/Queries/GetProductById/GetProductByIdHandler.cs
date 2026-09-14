namespace Inventory.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdHandler
{
    private readonly IProductReadStore _store;

    public GetProductByIdHandler(IProductReadStore store)
    {
        _store = store;
    }

    public Task<ProductDto?> HandleAsync(
        GetProductByIdQuery query,
        CancellationToken cancellationToken)
    {
        return _store.GetByIdAsync(
            query.Id,
            cancellationToken);
    }
}
