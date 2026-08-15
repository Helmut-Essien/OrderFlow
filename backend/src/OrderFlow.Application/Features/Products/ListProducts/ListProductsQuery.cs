using MediatR;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Application.Features.Products.ListProducts;

/// <summary>Paged product list for the authenticated shop. <see cref="PageSize"/> is 1–100 (handler default 20).</summary>
public sealed record ListProductsQuery(
    string? Search,
    string? Category,
    int Page,
    int PageSize) : IRequest<ProductListResponse>;
