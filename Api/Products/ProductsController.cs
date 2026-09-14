using Inventory.Api.Categories;
using Inventory.Application.Products;
using Inventory.Application.Products.Commands.CreateProduct;
using Inventory.Application.Products.Commands.DeleteProduct;
using Inventory.Application.Products.Commands.UpdateProduct;
using Inventory.Application.Products.Queries.GetProductById;
using Inventory.Application.Products.Queries.GetProducts;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Products;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    private readonly CreateProductHandler _createHandler;
    private readonly UpdateProductHandler _updateHandler;
    private readonly DeleteProductHandler _deleteHandler;
    private readonly GetProductsHandler _getProductsHandler;
    private readonly GetProductByIdHandler _getProductByIdHandler;

    public ProductsController(
        CreateProductHandler createHandler,
        UpdateProductHandler updateHandler,
        DeleteProductHandler deleteHandler,
        GetProductsHandler getProductsHandler,
        GetProductByIdHandler getProductByIdHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _getProductsHandler = getProductsHandler;
        _getProductByIdHandler = getProductByIdHandler;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand
        {
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            CategoryId = request.CategoryId,
            IsActive = request.IsActive
        };

        var result = await _createHandler.HandleAsync(
            command,
            cancellationToken);

        return BuildWriteResponse(
            result,
            product => CreatedAtRoute(
                "GetProductById",
                new { id = product.Id },
                product));
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sku = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductsQuery
        {
            Page = NormalizePage(page),
            PageSize = NormalizePageSize(pageSize),
            Search = search,
            Sku = sku,
            CategoryId = categoryId,
            IsActive = isActive
        };

        var result = await _getProductsHandler.HandleAsync(
            query,
            cancellationToken);

        return Ok(PagedResponse<ProductDto>.FromPagedResult(result));
    }

    [HttpGet("{id:guid}", Name = "GetProductById")]
    public async Task<IActionResult> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery { Id = id };

        var product = await _getProductByIdHandler.HandleAsync(
            query,
            cancellationToken);

        return product is null
            ? NotFound()
            : Ok(product);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            CategoryId = request.CategoryId,
            IsActive = request.IsActive
        };

        var result = await _updateHandler.HandleAsync(
            command,
            cancellationToken);

        return BuildWriteResponse(
            result,
            product => Ok(product));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand { Id = id };

        var status = await _deleteHandler.HandleAsync(
            command,
            cancellationToken);

        return status == ProductDeleteStatus.Deleted
            ? NoContent()
            : NotFound();
    }

    private IActionResult BuildWriteResponse(
        ProductWriteResult result,
        Func<ProductDto, IActionResult> onSuccess)
    {
        if (result.Success)
        {
            return onSuccess(result.Product!);
        }

        return result.ErrorType switch
        {
            ProductWriteErrorType.ProductNotFound => NotFound(),
            ProductWriteErrorType.CategoryUnavailable => Conflict(
                "The specified category does not exist, is inactive or is deleted."),
            ProductWriteErrorType.DuplicateSku => Conflict(
                "The specified SKU is already in use."),
            _ => Conflict()
        };
    }

    private static int NormalizePage(int page)
    {
        return page < MinPageSize
            ? MinPageSize
            : page;
    }

    private static int NormalizePageSize(int pageSize)
    {
        if (pageSize < MinPageSize)
        {
            return MinPageSize;
        }

        return pageSize > MaxPageSize
            ? MaxPageSize
            : pageSize;
    }
}
