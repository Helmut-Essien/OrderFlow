namespace OrderFlow.Shared.DTOs.Common;

/// <summary>Standard page envelope. <see cref="Page"/> is 1-based; <see cref="PageSize"/> is 1–100 for products.</summary>
public class PagedResult<T>
{
    /// <summary>Current page of items (never null; empty list when none).</summary>
    public required IReadOnlyList<T> Items { get; set; }

    /// <summary>Unpaged total matching the filter (not just this page).</summary>
    public required int TotalCount { get; set; }

    /// <summary>1-based page index.</summary>
    public required int Page { get; set; }

    /// <summary>Page size actually used (products: 1–100).</summary>
    public required int PageSize { get; set; }
}
