using Inventory.Application.Common;

namespace Inventory.Api.Categories;

public sealed record PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalItems { get; init; }
    public required int TotalPages { get; init; }

    public static PagedResponse<T> FromPagedResult(PagedResult<T> result)
    {
        return new PagedResponse<T>
        {
            Items = result.Items,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems,
            TotalPages = result.TotalPages
        };
    }
}
