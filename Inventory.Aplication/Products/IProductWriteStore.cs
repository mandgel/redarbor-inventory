namespace Inventory.Application.Products;

public interface IProductWriteStore
{
    Task<ProductWriteResult> CreateAsync(
        ProductWriteData data,
        CancellationToken cancellationToken);

    Task<ProductWriteResult> UpdateAsync(
        Guid id,
        ProductWriteData data,
        CancellationToken cancellationToken);

    Task<ProductDeleteStatus> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken);
}
