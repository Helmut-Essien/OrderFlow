namespace OrderFlow.Shared.DTOs.Dashboard;

/// <summary>
/// Shop home KPIs. <see cref="TodaysSales"/>, <see cref="OrderCount"/>, and <see cref="PendingWhatsAppCount"/> are 0 until those slices exist.
/// </summary>
public class DashboardDto
{
    /// <summary>Today's paid sales in GHS. 0 until orders exist.</summary>
    public required decimal TodaysSales { get; set; }

    public required int OrderCount { get; set; }

    /// <summary>Unclarified WhatsApp drafts. Gold emphasis in UI when greater than 0.</summary>
    public required int PendingWhatsAppCount { get; set; }

    public required int LowStockCount { get; set; }

    public required IReadOnlyList<LowStockItemDto> LowStock { get; set; }
}

/// <summary>Active product whose stock is at or below its low-stock threshold.</summary>
public class LowStockItemDto
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    public required string Sku { get; set; }

    public required int Stock { get; set; }

    public required int LowStockThreshold { get; set; }
}
