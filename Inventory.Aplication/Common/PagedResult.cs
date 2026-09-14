namespace Inventory.Application.Common;

public sealed record PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalItems { get; init; }

    public int TotalPages =>
        PageSize == 0
            ? 0
            : (int)Math.Ceiling(TotalItems / (double)PageSize);
}
