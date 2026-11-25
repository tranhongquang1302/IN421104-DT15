namespace LangCenter.Application.Common;

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public readonly record struct PageRequest(int Page, int PageSize)
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    public static PageRequest Of(int page, int pageSize) =>
        new(Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize));

    public int Skip => (int)Math.Min(((long)Page - 1) * PageSize, int.MaxValue);
}
