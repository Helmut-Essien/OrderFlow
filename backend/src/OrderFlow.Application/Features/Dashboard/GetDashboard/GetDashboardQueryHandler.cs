using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Shared.DTOs.Dashboard;

namespace OrderFlow.Application.Features.Dashboard.GetDashboard;

/// <summary>
/// Builds dashboard numbers for the JWT shop. Low-stock is live; sales/orders/WhatsApp are placeholders until slice 3+.
/// </summary>
public sealed class GetDashboardQueryHandler(
    ICurrentUser currentUser,
    IProductRepository products) : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    /// <summary>Returns live low-stock plus placeholder sales/orders/WhatsApp counts until those slices exist.</summary>
    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.ShopId))
            throw new UnauthorizedAppException("Not authenticated.");

        var lowStock = await products.GetLowStockAsync(currentUser.ShopId, cancellationToken);

        return new DashboardDto
        {
            // Honest zeros until orders/WhatsApp slices exist — do not invent sample charts.
            TodaysSales = 0m,
            OrderCount = 0,
            PendingWhatsAppCount = 0,
            LowStockCount = lowStock.Count,
            LowStock = lowStock
        };
    }
}
