using MediatR;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Application.Features.Products.UpdateProduct;

/// <summary>
/// Updates catalog fields for a product. Does not change stock; requires <see cref="ExpectedVersion"/> for concurrency.
/// </summary>
public sealed record UpdateProductCommand(
    string ProductId,
    string Name,
    string Sku,
    string? Category,
    decimal Price,
    int LowStockThreshold,
    bool IsActive,
    long ExpectedVersion) : IRequest<ProductDto>;
