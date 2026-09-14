namespace Inventory.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductHandler
{
    private readonly IProductWriteStore _store;

    public UpdateProductHandler(IProductWriteStore store)
    {
        _store = store;
    }

    public Task<ProductWriteResult> HandleAsync(
        UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        var data = new ProductWriteData
        {
            Name = command.Name,
            Description = command.Description,
            Sku = command.Sku,
            CategoryId = command.CategoryId,
            IsActive = command.IsActive
        };

        return _store.UpdateAsync(
            command.Id,
            data,
            cancellationToken);
    }
}
