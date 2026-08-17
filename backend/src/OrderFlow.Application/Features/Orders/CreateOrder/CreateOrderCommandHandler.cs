using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.Shared.DTOs.Orders;

namespace OrderFlow.Application.Features.Orders.CreateOrder;

/// <summary>
/// Creates a manual order, enforcing monthly plan caps. Optional confirm reserves stock with the same atomic SQL as stock adjust.
/// </summary>
/// <exception cref="UnauthorizedAppException">JWT is missing a shop id.</exception>
/// <exception cref="ForbiddenAppException">Shop is at <c>PlanQuota.MaxOrdersPerMonth</c>.</exception>
/// <exception cref="NotFoundAppException">A line product is missing or belongs to another shop.</exception>
/// <exception cref="ConflictAppException">Inactive product or insufficient stock on confirm.</exception>
/// <exception cref="ConcurrencyAppException">Stock version changed while reserving.</exception>
public sealed class CreateOrderCommandHandler(
    ICurrentUser currentUser,
    IShopRepository shops,
    IProductRepository products,
    IOrderRepository orders,
    IStockMovementRepository stockMovements,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateOrderCommand, OrderDto>
{
    /// <summary>
    /// Inserts the order (and reserves stock when confirming) in one transaction so a failed reserve cannot leave a Confirmed row.
    /// </summary>
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var shopId = RequireShopId(currentUser);
        OrderDto? dto = null;

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await shops.AcquirePlanCapLockAsync(shopId, ct);

            var shop = await shops.GetByIdAsync(shopId, ct)
                ?? throw new NotFoundAppException("Shop not found.");

            // Plan caps live here (not Domain) so quota can change with Platform planName.
            var quota = PlanQuota.FromPlanName(shop.PlanName);
            if (quota.MaxOrdersPerMonth is int maxOrders)
            {
                var now = DateTime.UtcNow;
                var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var monthEnd = monthStart.AddMonths(1);
                var count = await orders.CountCreatedInRangeAsync(shopId, monthStart, monthEnd, ct);
                if (count >= maxOrders)
                    throw new ForbiddenAppException($"Your {quota.Name} plan allows up to {maxOrders} orders this month.");
            }

            var productIds = request.Lines.Select(l => l.ProductId.Trim()).ToList();
            var catalog = await products.GetByIdsAsync(productIds, ct);
            var byId = catalog.ToDictionary(p => p.Id, StringComparer.Ordinal);

            var drafts = new List<OrderLineDraft>(request.Lines.Count);
            foreach (var line in request.Lines)
            {
                if (!byId.TryGetValue(line.ProductId.Trim(), out var product))
                    throw new NotFoundAppException("Product not found.");

                if (!product.IsActive)
                    throw new ConflictAppException($"{product.Sku} is inactive and cannot be sold.");

                drafts.Add(new OrderLineDraft(
                    product.Id,
                    product.Name,
                    product.Sku,
                    line.Quantity,
                    product.Price));
            }

            var order = Order.CreateManual(
                shopId,
                request.CustomerName,
                request.CustomerPhone,
                request.Notes,
                drafts,
                currentUser.UserId);

            if (request.ConfirmImmediately)
                order.TransitionTo(OrderStatus.Confirmed, DateTime.UtcNow);

            orders.Add(order);

            if (request.ConfirmImmediately)
            {
                await OrderStock.ReserveAsync(
                    products,
                    stockMovements,
                    order,
                    shopId,
                    currentUser.UserId,
                    ct);
            }

            await unitOfWork.SaveChangesAsync(ct);
            dto = OrderMapping.ToDto(order);
        }, cancellationToken);

        return dto!;
    }

    private static string RequireShopId(ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.ShopId))
            throw new UnauthorizedAppException("Not authenticated.");

        return currentUser.ShopId;
    }
}
