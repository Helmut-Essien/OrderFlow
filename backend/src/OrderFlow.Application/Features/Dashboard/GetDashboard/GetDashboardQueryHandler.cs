using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Shared.DTOs.Dashboard;

namespace OrderFlow.Application.Features.Dashboard.GetDashboard;

/// <summary>
/// Builds dashboard numbers for the JWT shop. Low-stock and order KPIs are SQL aggregations; pending WhatsApp is 0 until that slice writes those rows.
/// </summary>
public sealed class GetDashboardQueryHandler(
    ICurrentUser currentUser,
    IProductRepository products,
    IOrderRepository orders) : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    /// <summary>
    /// Returns live low-stock plus today’s still-paid sales/count (UTC date of <c>PaidAt</c>, excluding Cancelled) and the 10 newest orders.
    /// </summary>
    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.ShopId))
            throw new UnauthorizedAppException("Not authenticated.");

        var shopId = currentUser.ShopId;
        var now = DateTime.UtcNow;
        var dayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        var lowStock = await products.GetLowStockAsync(shopId, cancellationToken);
        var orderStats = await orders.GetDashboardStatsAsync(shopId, dayStart, dayEnd, cancellationToken);

        return new DashboardDto
        {
            TodaysSales = orderStats.TodaysSales,
            OrderCount = orderStats.TodaysPaidOrderCount,
            PendingWhatsAppCount = orderStats.PendingWhatsAppCount,
            LowStockCount = lowStock.Count,
            LowStock = lowStock,
            RecentOrders = orderStats.RecentOrders
        };
    }
}
