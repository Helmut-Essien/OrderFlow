using MediatR;
using OrderFlow.Shared.DTOs.Orders;

namespace OrderFlow.Application.Features.Orders.ListOrders;

/// <summary>Paged order list for the authenticated shop. <see cref="PageSize"/> is 1–100 (handler default 20).</summary>
public sealed record ListOrdersQuery(
    string? Search,
    string? Status,
    int Page,
    int PageSize) : IRequest<OrderListResponse>;
