using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Shared.DTOs.Orders;

namespace OrderFlow.Application.Features.Orders.ListOrders;

/// <summary>Lists orders for the JWT shop. Newest first; search matches customer name or phone in SQL.</summary>
public sealed class ListOrdersQueryHandler(
    ICurrentUser currentUser,
    IOrderRepository orders) : IRequestHandler<ListOrdersQuery, OrderListResponse>
{
    /// <summary>Pages the JWT shop orders. Status filter is the enum name when provided.</summary>
    public async Task<OrderListResponse> Handle(ListOrdersQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.ShopId))
            throw new UnauthorizedAppException("Not authenticated.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var result = await orders.ListAsync(
            currentUser.ShopId,
            request.Search,
            request.Status,
            page,
            pageSize,
            cancellationToken);

        return new OrderListResponse
        {
            Items = result.Items,
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
