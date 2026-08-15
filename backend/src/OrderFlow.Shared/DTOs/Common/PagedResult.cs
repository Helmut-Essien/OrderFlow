namespace OrderFlow.Shared.DTOs.Common;

/// <summary>Standard page envelope. <see cref="Page"/> is 1-based; <see cref="PageSize"/> is 1–100 for products.</summary>
public class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; set; }

    public required int TotalCount { get; set; }

    public required int Page { get; set; }

    public required int PageSize { get; set; }
}
