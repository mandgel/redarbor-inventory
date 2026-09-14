namespace Inventory.Application.Products;

public sealed record ProductWriteResult
{
    public required bool Success { get; init; }
    public ProductDto? Product { get; init; }
    public ProductWriteErrorType ErrorType { get; init; }

    public static ProductWriteResult Succeeded(ProductDto product)
    {
        return new ProductWriteResult
        {
            Success = true,
            Product = product,
            ErrorType = ProductWriteErrorType.None
        };
    }

    public static ProductWriteResult Failed(ProductWriteErrorType errorType)
    {
        return new ProductWriteResult
        {
            Success = false,
            ErrorType = errorType
        };
    }
}
