using MediatR;
using OrderFlow.Shared.DTOs.Orders;

namespace OrderFlow.Application.Features.Orders.GetOrder;

/// <summary>Returns one order with lines. Missing ids are 404 because EF shop filters hide other tenants' rows.</summary>
public sealed record GetOrderQuery(string OrderId) : IRequest<OrderDto>;
