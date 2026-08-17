using OrderFlow.Domain.Entities;
using OrderFlow.Shared.DTOs.Orders;

namespace OrderFlow.Application.Features.Orders;

/// <summary>Maps domain orders to public DTOs inside the handler, never in the controller.</summary>
internal static class OrderMapping
{
    public static OrderDto ToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            ShopId = order.ShopId,
            CustomerName = order.CustomerName,
            CustomerPhone = order.CustomerPhone,
            Notes = order.Notes,
            Status = order.Status.ToString(),
            Source = order.Source.ToString(),
            NeedsClarification = order.NeedsClarification,
            TotalAmount = order.TotalAmount,
            Version = order.Version,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            ConfirmedAt = order.ConfirmedAt,
            PaidAt = order.PaidAt,
            FulfilledAt = order.FulfilledAt,
            CancelledAt = order.CancelledAt,
            Lines = order.Lines
                .OrderBy(l => l.Sku)
                .Select(l => new OrderLineDto
                {
                    Id = l.Id,
                    ProductId = l.ProductId,
                    ProductName = l.ProductName,
                    Sku = l.Sku,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    LineTotal = l.LineTotal
                })
                .ToList()
        };
    }
}
