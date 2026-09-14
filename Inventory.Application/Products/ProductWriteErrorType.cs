namespace Inventory.Application.Products;

public enum ProductWriteErrorType
{
    None = 0,
    ProductNotFound = 1,
    CategoryUnavailable = 2,
    DuplicateSku = 3
}
