namespace OrderFlow.Shared.DTOs.Dashboard;

/// <summary>
/// Shop home KPIs. Sales and paid-order counts use Ghana/UTC calendar days of <c>PaidAt</c>
/// for orders still <c>Paid</c> or <c>Fulfilled</c> (cancelled sales are excluded).
/// </summary>
public class DashboardDto
{
    /// <summary>Sum of <c>TotalAmount</c> for today’s still-paid sales (UTC date of <c>PaidAt</c>), in GHS.</summary>
    public required decimal TodaysSales { get; set; }

    /// <summary>Count of <c>Paid</c>/<c>Fulfilled</c> orders whose <c>PaidAt</c> falls on today's UTC date.</summary>
    public required int OrderCount { get; set; }

    /// <summary>WhatsApp orders still Pending. Gold emphasis in UI when greater than 0.</summary>
    public required int PendingWhatsAppCount { get; set; }

    /// <summary>Active SKUs at or below their low-stock threshold.</summary>
    public required int LowStockCount { get; set; }

    /// <summary>Up to 50 low-stock rows for the dashboard list.</summary>
    public required IReadOnlyList<LowStockItemDto> LowStock { get; set; }

    /// <summary>Newest orders first, capped at 10. Empty when the shop has none.</summary>
    public required IReadOnlyList<DashboardOrderDto> RecentOrders { get; set; }
}

/// <summary>Compact order row for the dashboard “recent orders” list.</summary>
public class DashboardOrderDto
{
    /// <summary>Order ULID.</summary>
    public required string Id { get; set; }

    /// <summary>Customer display name.</summary>
    public required string CustomerName { get; set; }

    /// <summary>Lifecycle status name.</summary>
    public required string Status { get; set; }

    /// <summary>Entry channel name.</summary>
    public required string Source { get; set; }

    /// <summary>Sum of line totals in GHS.</summary>
    public required decimal TotalAmount { get; set; }

    /// <summary>UTC insert time.</summary>
    public required DateTime CreatedAt { get; set; }
}

/// <summary>Active product whose stock is at or below its low-stock threshold.</summary>
public class LowStockItemDto
{
    /// <summary>Product ULID.</summary>
    public required string Id { get; set; }

    /// <summary>Display name.</summary>
    public required string Name { get; set; }

    /// <summary>Uppercase SKU.</summary>
    public required string Sku { get; set; }

    /// <summary>On-hand quantity.</summary>
    public required int Stock { get; set; }

    /// <summary>Threshold that triggered the low-stock flag.</summary>
    public required int LowStockThreshold { get; set; }
}
