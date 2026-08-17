using OrderFlow.Shared.DTOs.Common;

namespace OrderFlow.Shared.DTOs.Orders;

/// <summary>Paged order list row without lines. Use GET by id for the full snapshot.</summary>
public class OrderListDto
{
    /// <summary>Order ULID.</summary>
    public required string Id { get; set; }

    /// <summary>Tenant shop that owns this order.</summary>
    public required string ShopId { get; set; }

    /// <summary>Customer display name.</summary>
    public required string CustomerName { get; set; }

    /// <summary>Optional customer phone.</summary>
    public string? CustomerPhone { get; set; }

    /// <summary>Lifecycle status name.</summary>
    public required string Status { get; set; }

    /// <summary>Entry channel name.</summary>
    public required string Source { get; set; }

    /// <summary>WhatsApp unmatched-text flag.</summary>
    public required bool NeedsClarification { get; set; }

    /// <summary>Sum of line totals in GHS.</summary>
    public required decimal TotalAmount { get; set; }

    /// <summary>Number of lines (not loaded on the list).</summary>
    public required int LineCount { get; set; }

    /// <summary>Send back as <c>expectedVersion</c> on status changes.</summary>
    public required long Version { get; set; }

    /// <summary>UTC insert time.</summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>UTC last status mutation.</summary>
    public required DateTime UpdatedAt { get; set; }
}

/// <summary>Paged order list. <see cref="PagedResult{T}.PageSize"/> is 1–100 (default 20).</summary>
public class OrderListResponse : PagedResult<OrderListDto>;
