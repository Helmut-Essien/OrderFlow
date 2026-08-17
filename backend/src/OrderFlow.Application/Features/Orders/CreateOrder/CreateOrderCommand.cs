using MediatR;
using OrderFlow.Shared.DTOs.Orders;

namespace OrderFlow.Application.Features.Orders.CreateOrder;

/// <summary>One SKU on a create-order command. Quantity only — price is snapshotted from the product.</summary>
public sealed record CreateOrderLineInput(string ProductId, int Quantity);

/// <summary>
/// Creates a Manual order. When <see cref="ConfirmImmediately"/> is true, status becomes Confirmed and stock is reserved in the same transaction.
/// </summary>
public sealed record CreateOrderCommand(
    string CustomerName,
    string? CustomerPhone,
    string? Notes,
    bool ConfirmImmediately,
    IReadOnlyList<CreateOrderLineInput> Lines) : IRequest<OrderDto>;
