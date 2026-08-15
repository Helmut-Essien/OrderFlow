using MediatR;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Application.Features.Products.GetProduct;

/// <summary>Loads one product by id in the authenticated shop.</summary>
public sealed record GetProductQuery(string ProductId) : IRequest<ProductDto>;
