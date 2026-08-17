using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Shared.DTOs.Dashboard;
using OrderFlow.Shared.DTOs.Orders;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Order persistence. List/dashboard reads are untracked SQL projections; status changes use <see cref="GetTrackedByIdAsync"/>.
/// </summary>
public sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    /// <inheritdoc />
    public Task<Order?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return db.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Order?> GetTrackedByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OrderListResult> ListAsync(
        string shopId,
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = db.Orders.AsNoTracking().Where(o => o.ShopId == shopId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ILike keeps the expression sargable-friendly vs ToLower().Contains, which cannot use indexes.
            var pattern = ToILikeContainsPattern(search);
            query = query.Where(o =>
                EF.Functions.ILike(o.CustomerName, pattern, "\\") ||
                (o.CustomerPhone != null && EF.Functions.ILike(o.CustomerPhone, pattern, "\\")));
        }

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsed)
            && Enum.GetNames<OrderStatus>().Any(n => n.Equals(status, StringComparison.OrdinalIgnoreCase)))
        {
            query = query.Where(o => o.Status == parsed);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderListDto
            {
                Id = o.Id,
                ShopId = o.ShopId,
                CustomerName = o.CustomerName,
                CustomerPhone = o.CustomerPhone,
                Status = o.Status.ToString(),
                Source = o.Source.ToString(),
                NeedsClarification = o.NeedsClarification,
                TotalAmount = o.TotalAmount,
                LineCount = o.Lines.Count,
                Version = o.Version,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new OrderListResult(items, total);
    }

    /// <inheritdoc />
    public Task<int> CountCreatedInRangeAsync(
        string shopId,
        DateTime monthStartUtc,
        DateTime monthEndUtc,
        CancellationToken cancellationToken = default)
    {
        return db.Orders.CountAsync(
            o => o.ShopId == shopId && o.CreatedAt >= monthStartUtc && o.CreatedAt < monthEndUtc,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OrderDashboardStats> GetDashboardStatsAsync(
        string shopId,
        DateTime dayStartUtc,
        DateTime dayEndUtc,
        CancellationToken cancellationToken = default)
    {
        var paidToday = db.Orders.AsNoTracking().Where(o =>
            o.ShopId == shopId
            && o.PaidAt != null
            && o.PaidAt >= dayStartUtc
            && o.PaidAt < dayEndUtc
            // PaidAt is kept on cancel for audit; only Paid/Fulfilled are still a sale.
            && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Fulfilled));

        var todaysSales = await paidToday.SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;
        var todaysPaidCount = await paidToday.CountAsync(cancellationToken);

        var pendingWhatsApp = await db.Orders.AsNoTracking().CountAsync(
            o => o.ShopId == shopId
                && o.Source == OrderSource.WhatsApp
                && o.Status == OrderStatus.Pending,
            cancellationToken);

        var recent = await db.Orders
            .AsNoTracking()
            .Where(o => o.ShopId == shopId)
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Take(10)
            .Select(o => new DashboardOrderDto
            {
                Id = o.Id,
                CustomerName = o.CustomerName,
                Status = o.Status.ToString(),
                Source = o.Source.ToString(),
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new OrderDashboardStats(todaysSales, todaysPaidCount, pendingWhatsApp, recent);
    }

    /// <inheritdoc />
    public void Add(Order order) => db.Orders.Add(order);

    /// <summary>Wraps user search in <c>%...%</c> and escapes LIKE wildcards so they are literal.</summary>
    private static string ToILikeContainsPattern(string search)
    {
        var escaped = search.Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }
}
