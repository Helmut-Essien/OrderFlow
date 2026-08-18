using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.Shared.DTOs.Orders;

namespace OrderFlow.Application.Features.Orders.ChangeOrderStatus;

/// <summary>
/// Applies an allowed status transition and the matching stock movement in one transaction.
/// </summary>
/// <exception cref="ConcurrencyAppException">Stale <c>expectedVersion</c> or stock version changed during reserve/release.</exception>
/// <exception cref="ConflictAppException">Illegal transition or insufficient stock on confirm.</exception>
public sealed class ChangeOrderStatusCommandHandler(
    ICurrentUser currentUser,
    IOrderRepository orders,
    IProductRepository products,
    IStockMovementRepository stockMovements,
    IUnitOfWork unitOfWork) : IRequestHandler<ChangeOrderStatusCommand, OrderDto>
{
    /// <summary>
    /// Reserves on Confirmed, writes Deduct audit on Paid (stock already held), releases on Cancelled from Confirmed or Paid.
    /// </summary>
    public async Task<OrderDto> Handle(ChangeOrderStatusCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.ShopId))
            throw new UnauthorizedAppException("Not authenticated.");

        var shopId = currentUser.ShopId;
        var target = Enum.Parse<OrderStatus>(request.Status, ignoreCase: true);
        OrderDto? dto = null;

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = await orders.GetTrackedByIdAsync(request.OrderId, ct)
                ?? throw new NotFoundAppException("Order not found.");

            if (order.ShopId != shopId)
                throw new NotFoundAppException("Order not found.");

            if (order.Version != request.ExpectedVersion)
                throw new ConcurrencyAppException("This order was updated by someone else. Refresh and try again.");

            if (!Order.CanTransition(order.Status, target))
                throw new ConflictAppException($"Cannot change a {order.Status} order to {target}.");

            var from = order.Status;
            order.TransitionTo(target, DateTime.UtcNow);

            if (from == OrderStatus.Pending && target == OrderStatus.Confirmed)
            {
                await OrderStock.ReserveAsync(products, stockMovements, unitOfWork, order, shopId, currentUser.UserId, ct);
            }
            else if (from == OrderStatus.Confirmed && target == OrderStatus.Paid)
            {
                await OrderStock.WriteDeductAuditAsync(products, stockMovements, order, shopId, currentUser.UserId, ct);
            }
            else if (target == OrderStatus.Cancelled && from is OrderStatus.Confirmed or OrderStatus.Paid)
            {
                await OrderStock.ReleaseAsync(products, stockMovements, unitOfWork, order, shopId, currentUser.UserId, ct);
            }

            await unitOfWork.SaveChangesAsync(ct);
            dto = OrderMapping.ToDto(order);
        }, cancellationToken);

        return dto!;
    }
}
