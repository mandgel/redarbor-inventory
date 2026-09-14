namespace Inventory.Application.Products.Commands.CreateProduct;

public sealed class CreateProductHandler
{
    private readonly IProductWriteStore _store;

    public CreateProductHandler(IProductWriteStore store)
    {
        _store = store;
    }

    public Task<ProductWriteResult> HandleAsync(
        CreateProductCommand command,
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

        return _store.CreateAsync(data, cancellationToken);
    }
}
