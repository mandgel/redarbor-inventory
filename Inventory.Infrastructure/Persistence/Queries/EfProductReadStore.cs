using Inventory.Application.Common;
using Inventory.Application.Products;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Queries;

public sealed class EfProductReadStore : IProductReadStore
{
    private readonly InventoryDbContext _dbContext;

    public EfProductReadStore(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await BuildProjectionQuery()
            .SingleOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);

        return product;
    }

    public async Task<PagedResult<ProductDto>> GetPagedAsync(
        ProductQueryFilter filter,
        CancellationToken cancellationToken)
    {
        var query = ApplyFilters(BuildProjectionQuery(), filter)
            .OrderBy(product => product.Name);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>
        {
            Items = items,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalItems = totalItems
        };
    }

    private IQueryable<ProductDto> BuildProjectionQuery()
    {
        return _dbContext.Products
            .AsNoTracking()
            .Join(
                _dbContext.Categories.AsNoTracking(),
                product => product.CategoryId,
                category => category.Id,
                (product, category) => new { product, category })
            .Join(
                _dbContext.InventoryBalances.AsNoTracking(),
                joined => joined.product.Id,
                balance => balance.ProductId,
                (joined, balance) => new ProductDto
                {
                    Id = joined.product.Id,
                    Name = joined.product.Name,
                    Description = joined.product.Description,
                    Sku = joined.product.Sku,
                    CategoryId = joined.product.CategoryId,
                    CategoryName = joined.category.Name,
                    IsActive = joined.product.IsActive,
                    CurrentStock = balance.CurrentStock,
                    CreatedAt = joined.product.CreatedAt,
                    UpdatedAt = joined.product.UpdatedAt
                });
    }

    private static IQueryable<ProductDto> ApplyFilters(
        IQueryable<ProductDto> query,
        ProductQueryFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            query = query.Where(product =>
                product.Name.Contains(filter.Search) ||
                product.Sku.Contains(filter.Search));
        }

        if (!string.IsNullOrWhiteSpace(filter.Sku))
        {
            query = query.Where(product => product.Sku == filter.Sku);
        }

        if (filter.CategoryId is not null)
        {
            query = query.Where(
                product => product.CategoryId == filter.CategoryId);
        }

        if (filter.IsActive is not null)
        {
            query = query.Where(
                product => product.IsActive == filter.IsActive);
        }

        return query;
    }
}
