using Inventory.Application.Categories.Commands.CreateCategory;
using Inventory.Application.Categories.Commands.DeleteCategory;
using Inventory.Application.Categories.Commands.UpdateCategory;
using Inventory.Application.Categories.Queries.GetCategories;
using Inventory.Application.Categories.Queries.GetCategoryById;
using Inventory.Application.Categories;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Categories;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController : ControllerBase
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;
    private readonly CreateCategoryHandler _createHandler;
    private readonly UpdateCategoryHandler _updateHandler;
    private readonly DeleteCategoryHandler _deleteHandler;
    private readonly GetCategoriesHandler _getCategoriesHandler;
    private readonly GetCategoryByIdHandler _getCategoryByIdHandler;

    public CategoriesController(
        CreateCategoryHandler createHandler,
        UpdateCategoryHandler updateHandler,
        DeleteCategoryHandler deleteHandler,
        GetCategoriesHandler getCategoriesHandler,
        GetCategoryByIdHandler getCategoryByIdHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _getCategoriesHandler = getCategoriesHandler;
        _getCategoryByIdHandler = getCategoryByIdHandler;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCategoryCommand
        {
            Name = request.Name,
            IsActive = request.IsActive
        };

        var category = await _createHandler.HandleAsync(
            command,
            cancellationToken);

        return CreatedAtRoute(
             "GetCategoryById",
             new { id = category.Id },
             category);
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = NormalizePage(page);
        var normalizedPageSize = NormalizePageSize(pageSize);
        var query = new GetCategoriesQuery
        {
            Page = normalizedPage,
            PageSize = normalizedPageSize
        };

        var result = await _getCategoriesHandler.HandleAsync(
            query,
            cancellationToken);

        return Ok(PagedResponse<CategoryDto>.FromPagedResult(result));
    }

    [HttpGet("{id:guid}", Name = "GetCategoryById")]
    public async Task<IActionResult> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetCategoryByIdQuery { Id = id };

        var category = await _getCategoryByIdHandler.HandleAsync(
            query,
            cancellationToken);

        return category is null
            ? NotFound()
            : Ok(category);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand
        {
            Id = id,
            Name = request.Name,
            IsActive = request.IsActive
        };

        var category = await _updateHandler.HandleAsync(
            command,
            cancellationToken);

        return category is null
            ? NotFound()
            : Ok(category);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteCategoryCommand { Id = id };

        var deleted = await _deleteHandler.HandleAsync(
            command,
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound();
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
