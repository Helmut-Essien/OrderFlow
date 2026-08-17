using MediatR;
using OrderFlow.Shared.DTOs.Orders;

namespace OrderFlow.Application.Features.Orders.ChangeOrderStatus;

/// <summary>
/// Moves an order along the lifecycle. Stock reserve/deduct/release runs in the same transaction as the status write.
/// </summary>
public sealed record ChangeOrderStatusCommand(
    string OrderId,
    string Status,
    long ExpectedVersion) : IRequest<OrderDto>;
