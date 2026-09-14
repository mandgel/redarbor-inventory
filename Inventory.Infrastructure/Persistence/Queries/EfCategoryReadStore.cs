using Inventory.Application.Categories;
using Inventory.Application.Common;
using Inventory.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Queries;

public sealed class EfCategoryReadStore : ICategoryReadStore
{
    private readonly InventoryDbContext _dbContext;

    public EfCategoryReadStore(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CategoryDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var category = await _dbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(
                category => category.Id == id,
                cancellationToken);

        return category is null
            ? null
            : CategoryDto.FromEntity(category);
    }

    public async Task<PagedResult<CategoryDto>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(category => new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<CategoryDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }
}
