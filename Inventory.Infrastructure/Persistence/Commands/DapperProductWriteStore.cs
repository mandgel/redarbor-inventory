using Dapper;
using Inventory.Application.Products;
using Microsoft.Data.SqlClient;

namespace Inventory.Infrastructure.Persistence.Commands;

public sealed class DapperProductWriteStore : IProductWriteStore
{
    private readonly string _connectionString;

    public DapperProductWriteStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ProductWriteResult> CreateAsync(
        ProductWriteData data,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction =
            (SqlTransaction)await connection.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var result = await CreateWithinTransactionAsync(
                connection,
                transaction,
                data,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ProductWriteResult> UpdateAsync(
        Guid id,
        ProductWriteData data,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return await UpdateProductAsync(
            connection,
            id,
            data,
            cancellationToken);
    }

    public async Task<ProductDeleteStatus> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return await SoftDeleteProductAsync(
            connection,
            id,
            cancellationToken);
    }

    private static async Task<ProductWriteResult> CreateWithinTransactionAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        ProductWriteData data,
        CancellationToken cancellationToken)
    {
        var validationError = await ValidateCategoryAndSkuAsync(
            connection,
            transaction,
            data,
            excludingProductId: null,
            cancellationToken);

        if (validationError is not null)
        {
            return ProductWriteResult.Failed(validationError.Value);
        }

        var product = await InsertProductAsync(
            connection,
            transaction,
            data,
            cancellationToken);

        await InsertInitialBalanceAsync(
            connection,
            transaction,
            product.Id,
            cancellationToken);

        return ProductWriteResult.Succeeded(product);
    }

    private static async Task<ProductWriteResult> UpdateProductAsync(
        SqlConnection connection,
        Guid id,
        ProductWriteData data,
        CancellationToken cancellationToken)
    {
        var productExists = await ProductExistsAsync(
            connection,
            id,
            cancellationToken);

        if (!productExists)
        {
            return ProductWriteResult.Failed(
                ProductWriteErrorType.ProductNotFound);
        }

        var validationError = await ValidateCategoryAndSkuAsync(
            connection,
            transaction: null,
            data,
            excludingProductId: id,
            cancellationToken);

        if (validationError is not null)
        {
            return ProductWriteResult.Failed(validationError.Value);
        }

        var product = await ApplyProductUpdateAsync(
            connection,
            id,
            data,
            cancellationToken);

        return ProductWriteResult.Succeeded(product!);
    }

    private static async Task<ProductWriteErrorType?> ValidateCategoryAndSkuAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        ProductWriteData data,
        Guid? excludingProductId,
        CancellationToken cancellationToken)
    {
        var categoryIsAvailable = await IsCategoryAvailableAsync(
            connection,
            transaction,
            data.CategoryId,
            cancellationToken);

        if (!categoryIsAvailable)
        {
            return ProductWriteErrorType.CategoryUnavailable;
        }

        var skuIsTaken = await IsSkuTakenAsync(
            connection,
            transaction,
            data.Sku,
            excludingProductId,
            cancellationToken);

        if (skuIsTaken)
        {
            return ProductWriteErrorType.DuplicateSku;
        }

        return null;
    }

    private static async Task<bool> ProductExistsAsync(
        SqlConnection connection,
        Guid id,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT 1
            FROM Products
            WHERE Id = @Id
              AND IsDeleted = 0;
            """;

        var command = new CommandDefinition(
            sql,
            new { Id = id },
            cancellationToken: cancellationToken);

        var exists = await connection.QuerySingleOrDefaultAsync<int?>(command);

        return exists is not null;
    }

    private static async Task<bool> IsCategoryAvailableAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT 1
            FROM Categories
            WHERE Id = @CategoryId
              AND IsDeleted = 0
              AND IsActive = 1;
            """;

        var command = new CommandDefinition(
            sql,
            new { CategoryId = categoryId },
            transaction,
            cancellationToken: cancellationToken);

        var exists = await connection.QuerySingleOrDefaultAsync<int?>(command);

        return exists is not null;
    }

    private static async Task<bool> IsSkuTakenAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string sku,
        Guid? excludingProductId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT 1
            FROM Products
            WHERE Sku = @Sku
              AND IsDeleted = 0
              AND (@ExcludingProductId IS NULL OR Id <> @ExcludingProductId);
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                Sku = sku,
                ExcludingProductId = excludingProductId
            },
            transaction,
            cancellationToken: cancellationToken);

        var exists = await connection.QuerySingleOrDefaultAsync<int?>(command);

        return exists is not null;
    }

    private static async Task<ProductDto> InsertProductAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        ProductWriteData data,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO Products
            (
                Id,
                Name,
                Description,
                Sku,
                CategoryId,
                IsActive,
                IsDeleted,
                CreatedAt,
                UpdatedAt
            )
            OUTPUT
                INSERTED.Id,
                INSERTED.Name,
                INSERTED.Description,
                INSERTED.Sku,
                INSERTED.CategoryId,
                INSERTED.IsActive,
                INSERTED.CreatedAt,
                INSERTED.UpdatedAt
            VALUES
            (
                @Id,
                @Name,
                @Description,
                @Sku,
                @CategoryId,
                @IsActive,
                0,
                @Timestamp,
                @Timestamp
            );
            """;

        var timestamp = DateTime.UtcNow;

        var parameters = new
        {
            Id = Guid.NewGuid(),
            data.Name,
            data.Description,
            data.Sku,
            data.CategoryId,
            data.IsActive,
            Timestamp = timestamp
        };

        var command = new CommandDefinition(
            sql,
            parameters,
            transaction,
            cancellationToken: cancellationToken);

        var inserted = await connection
            .QuerySingleAsync<InsertedProduct>(command);

        return await LoadProductDtoAsync(
            connection,
            transaction,
            inserted,
            cancellationToken);
    }

    private static async Task<ProductDto> LoadProductDtoAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        InsertedProduct inserted,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.Name
            FROM Categories c
            WHERE c.Id = @CategoryId;
            """;

        var command = new CommandDefinition(
            sql,
            new { inserted.CategoryId },
            transaction,
            cancellationToken: cancellationToken);

        var categoryName = await connection.QuerySingleAsync<string>(command);

        return new ProductDto
        {
            Id = inserted.Id,
            Name = inserted.Name,
            Description = inserted.Description,
            Sku = inserted.Sku,
            CategoryId = inserted.CategoryId,
            CategoryName = categoryName,
            IsActive = inserted.IsActive,
            CurrentStock = 0m,
            CreatedAt = inserted.CreatedAt,
            UpdatedAt = inserted.UpdatedAt
        };
    }

    private static async Task InsertInitialBalanceAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        Guid productId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO InventoryBalances
            (
                ProductId,
                CurrentStock,
                UpdatedAt
            )
            VALUES
            (
                @ProductId,
                0,
                @UpdatedAt
            );
            """;

        var command = new CommandDefinition(
            sql,
            new
            {
                ProductId = productId,
                UpdatedAt = DateTime.UtcNow
            },
            transaction,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    private static async Task<ProductDto?> ApplyProductUpdateAsync(
        SqlConnection connection,
        Guid id,
        ProductWriteData data,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE Products
            SET
                Name = @Name,
                Description = @Description,
                Sku = @Sku,
                CategoryId = @CategoryId,
                IsActive = @IsActive,
                UpdatedAt = @Timestamp
            OUTPUT
                INSERTED.Id,
                INSERTED.Name,
                INSERTED.Description,
                INSERTED.Sku,
                INSERTED.CategoryId,
                INSERTED.IsActive,
                INSERTED.CreatedAt,
                INSERTED.UpdatedAt
            WHERE Id = @Id
              AND IsDeleted = 0;
            """;

        var parameters = new
        {
            Id = id,
            data.Name,
            data.Description,
            data.Sku,
            data.CategoryId,
            data.IsActive,
            Timestamp = DateTime.UtcNow
        };

        var command = new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken);

        var updated = await connection
            .QuerySingleOrDefaultAsync<InsertedProduct>(command);

        if (updated is null)
        {
            return null;
        }

        var currentStock = await GetCurrentStockAsync(
            connection,
            id,
            cancellationToken);

        return await BuildUpdatedDtoAsync(
            connection,
            updated,
            currentStock,
            cancellationToken);
    }

    private static async Task<decimal> GetCurrentStockAsync(
        SqlConnection connection,
        Guid productId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CurrentStock
            FROM InventoryBalances
            WHERE ProductId = @ProductId;
            """;

        var command = new CommandDefinition(
            sql,
            new { ProductId = productId },
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<decimal>(command);
    }

    private static async Task<ProductDto> BuildUpdatedDtoAsync(
        SqlConnection connection,
        InsertedProduct updated,
        decimal currentStock,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.Name
            FROM Categories c
            WHERE c.Id = @CategoryId;
            """;

        var command = new CommandDefinition(
            sql,
            new { updated.CategoryId },
            cancellationToken: cancellationToken);

        var categoryName = await connection.QuerySingleAsync<string>(command);

        return new ProductDto
        {
            Id = updated.Id,
            Name = updated.Name,
            Description = updated.Description,
            Sku = updated.Sku,
            CategoryId = updated.CategoryId,
            CategoryName = categoryName,
            IsActive = updated.IsActive,
            CurrentStock = currentStock,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt
        };
    }

    private static async Task<ProductDeleteStatus> SoftDeleteProductAsync(
        SqlConnection connection,
        Guid id,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE Products
            SET
                IsDeleted = 1,
                IsActive = 0,
                DeletedAt = @Timestamp,
                UpdatedAt = @Timestamp
            WHERE Id = @Id
              AND IsDeleted = 0;
            """;

        var parameters = new
        {
            Id = id,
            Timestamp = DateTime.UtcNow
        };

        var command = new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken);

        var affectedRows = await connection.ExecuteAsync(command);

        return affectedRows > 0
            ? ProductDeleteStatus.Deleted
            : ProductDeleteStatus.NotFound;
    }
}
