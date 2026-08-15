using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Application.Features.Products.GetProduct;

/// <summary>Returns a product DTO. Missing ids are 404 because EF shop filters hide other tenants' rows.</summary>
public sealed class GetProductQueryHandler(
    ICurrentUser currentUser,
    IProductRepository products) : IRequestHandler<GetProductQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.ShopId))
            throw new UnauthorizedAppException("Not authenticated.");

        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundAppException("Product not found.");

        return ProductMapping.ToDto(product);
    }
}
