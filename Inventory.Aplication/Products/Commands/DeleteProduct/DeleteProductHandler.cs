namespace Inventory.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductHandler
{
    private readonly IProductWriteStore _store;

    public DeleteProductHandler(IProductWriteStore store)
    {
        _store = store;
    }

    public Task<ProductDeleteStatus> HandleAsync(
        DeleteProductCommand command,
        CancellationToken cancellationToken)
    {
        return _store.DeleteAsync(
            command.Id,
            cancellationToken);
    }
}
