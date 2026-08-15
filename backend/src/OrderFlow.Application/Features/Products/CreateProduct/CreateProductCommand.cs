using MediatR;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Application.Features.Products.CreateProduct;

/// <summary>Creates a catalog product in the authenticated shop. SKU is normalized to uppercase in the handler.</summary>
public sealed record CreateProductCommand(
    string Name,
    string Sku,
    string? Category,
    decimal Price,
    int Stock,
    int LowStockThreshold) : IRequest<ProductDto>;
