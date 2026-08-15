using MediatR;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Application.Features.Products.AdjustStock;

/// <summary>
/// Manual stock adjustment. <see cref="QuantityDelta"/> is signed; resulting stock must stay ≥ 0.
/// </summary>
public sealed record AdjustStockCommand(
    string ProductId,
    int QuantityDelta,
    long ExpectedVersion,
    string? Notes) : IRequest<ProductDto>;
