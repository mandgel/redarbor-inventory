using Dapper;
using Inventory.Application.Categories;
using Microsoft.Data.SqlClient;

namespace Inventory.Infrastructure.Persistence.Commands;

public sealed class DapperCategoryWriteStore : ICategoryWriteStore
{
    private readonly string _connectionString;

    public DapperCategoryWriteStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<CategoryDto> CreateAsync(
        string name,
        bool isActive,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return await InsertCategoryAsync(
            connection,
            name,
            isActive,
            cancellationToken);
    }

    public async Task<CategoryDto?> UpdateAsync(
        Guid id,
        string name,
        bool isActive,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return await UpdateCategoryAsync(
            connection,
            id,
            name,
            isActive,
            cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return await SoftDeleteCategoryAsync(
            connection,
            id,
            cancellationToken);
    }

    private static async Task<CategoryDto> InsertCategoryAsync(
        SqlConnection connection,
        string name,
        bool isActive,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO Categories
            (
                Id,
                Name,
                IsActive,
                IsDeleted,
                CreatedAt,
                UpdatedAt
            )
            OUTPUT
                INSERTED.Id,
                INSERTED.Name,
                INSERTED.IsActive,
                INSERTED.CreatedAt,
                INSERTED.UpdatedAt
            VALUES
            (
                @Id,
                @Name,
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
            Name = name,
            IsActive = isActive,
            Timestamp = timestamp
        };

        var command = new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleAsync<CategoryDto>(command);
    }

    private static async Task<CategoryDto?> UpdateCategoryAsync(
        SqlConnection connection,
        Guid id,
        string name,
        bool isActive,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE Categories
            SET
                Name = @Name,
                IsActive = @IsActive,
                UpdatedAt = @Timestamp
            OUTPUT
                INSERTED.Id,
                INSERTED.Name,
                INSERTED.IsActive,
                INSERTED.CreatedAt,
                INSERTED.UpdatedAt
            WHERE Id = @Id
              AND IsDeleted = 0;
            """;

        var parameters = new
        {
            Id = id,
            Name = name,
            IsActive = isActive,
            Timestamp = DateTime.UtcNow
        };

        var command = new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<CategoryDto>(command);
    }

    private static async Task<bool> SoftDeleteCategoryAsync(
        SqlConnection connection,
        Guid id,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE Categories
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

        return affectedRows > 0;
    }
}
