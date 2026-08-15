using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Application.Features.Products.ListProducts;

/// <summary>
/// Lists products for the JWT shop. Categories and active count are shop-wide so chips and plan caps stay accurate across pages.
/// </summary>
public sealed class ListProductsQueryHandler(
    ICurrentUser currentUser,
    IProductRepository products) : IRequestHandler<ListProductsQuery, ProductListResponse>
{
    public async Task<ProductListResponse> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.ShopId))
            throw new UnauthorizedAppException("Not authenticated.");

        var shopId = currentUser.ShopId;
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        // Sequential on purpose: one scoped DbContext is not safe for concurrent queries.
        var result = await products.ListAsync(shopId, request.Search, request.Category, page, pageSize, cancellationToken);
        var categories = await products.ListCategoriesAsync(shopId, cancellationToken);
        var activeCount = await products.CountByShopAsync(shopId, cancellationToken);

        return new ProductListResponse
        {
            Items = result.Items.Select(ProductMapping.ToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = pageSize,
            Categories = categories,
            ActiveCount = activeCount
        };
    }
}
