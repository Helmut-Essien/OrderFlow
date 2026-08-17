using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Shared.DTOs.Orders;

namespace OrderFlow.Application.Features.Orders.GetOrder;

/// <summary>Returns an order DTO. Missing ids are 404 because EF shop filters hide other tenants' rows.</summary>
public sealed class GetOrderQueryHandler(
    ICurrentUser currentUser,
    IOrderRepository orders) : IRequestHandler<GetOrderQuery, OrderDto>
{
    /// <summary>Returns 404 when the id is missing or belongs to another shop (EF filter hides the row).</summary>
    public async Task<OrderDto> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.ShopId))
            throw new UnauthorizedAppException("Not authenticated.");

        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundAppException("Order not found.");

        return OrderMapping.ToDto(order);
    }
}
