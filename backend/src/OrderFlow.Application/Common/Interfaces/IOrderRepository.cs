using OrderFlow.Domain.Entities;
using OrderFlow.Shared.DTOs.Dashboard;
using OrderFlow.Shared.DTOs.Orders;

namespace OrderFlow.Application.Common.Interfaces;

/// <summary>Page of order list rows plus the unpaged total. Items are projected in SQL.</summary>
public sealed record OrderListResult(IReadOnlyList<OrderListDto> Items, int TotalCount);

/// <summary>Dashboard order aggregations for one UTC calendar day. Recent rows are capped at 10.</summary>
public sealed record OrderDashboardStats(
    decimal TodaysSales,
    int TodaysPaidOrderCount,
    int PendingWhatsAppCount,
    IReadOnlyList<DashboardOrderDto> RecentOrders);

/// <summary>
/// Persistence port for shop-scoped orders. Implementations must honor EF global <c>ShopId</c> filters.
/// </summary>
public interface IOrderRepository
{
    /// <summary>Untracked read including lines for GET mapping. Do not mutate the result.</summary>
    Task<Order?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Tracked load including lines for status transitions so <c>SaveChanges</c> persists <c>TransitionTo</c>.</summary>
    Task<Order?> GetTrackedByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Paged list. Projects to <see cref="OrderListDto"/> in SQL (no line materialize).</summary>
    Task<OrderListResult> ListAsync(
        string shopId,
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Orders created in <paramref name="monthStartUtc"/>..<paramref name="monthEndUtc"/> (end exclusive).
    /// Used to enforce <c>PlanQuota.MaxOrdersPerMonth</c>; cancelled orders still consume a slot.
    /// </summary>
    Task<int> CountCreatedInRangeAsync(
        string shopId,
        DateTime monthStartUtc,
        DateTime monthEndUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Today’s paid sales/count for orders still <c>Paid</c> or <c>Fulfilled</c> (cancelled sales are excluded),
    /// pending WhatsApp drafts, and the 10 newest orders. All SQL.
    /// </summary>
    Task<OrderDashboardStats> GetDashboardStatsAsync(
        string shopId,
        DateTime dayStartUtc,
        DateTime dayEndUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Stages a new order (and its lines) for insert. Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist.</summary>
    void Add(Order order);
}
