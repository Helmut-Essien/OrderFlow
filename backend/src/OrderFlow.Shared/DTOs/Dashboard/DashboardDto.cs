namespace OrderFlow.Shared.DTOs.Dashboard;

/// <summary>
/// Shop home KPIs. <see cref="TodaysSales"/>, <see cref="OrderCount"/>, and <see cref="PendingWhatsAppCount"/> are 0 until those slices exist.
/// </summary>
public class DashboardDto
{
    /// <summary>Today's paid sales in GHS. 0 until orders exist.</summary>
    public required decimal TodaysSales { get; set; }

    /// <summary>Paid order count for today. 0 until orders exist.</summary>
    public required int OrderCount { get; set; }

    /// <summary>Unclarified WhatsApp drafts. Gold emphasis in UI when greater than 0.</summary>
    public required int PendingWhatsAppCount { get; set; }

    /// <summary>Active SKUs at or below their low-stock threshold.</summary>
    public required int LowStockCount { get; set; }

    /// <summary>Up to 50 low-stock rows for the dashboard list.</summary>
    public required IReadOnlyList<LowStockItemDto> LowStock { get; set; }
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
