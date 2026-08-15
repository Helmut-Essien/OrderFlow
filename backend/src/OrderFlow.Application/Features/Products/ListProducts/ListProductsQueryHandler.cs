using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Shared.DTOs.Common;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Application.Features.Products.ListProducts;

/// <summary>Lists products for the JWT shop. Search matches name or SKU; results are tenant-filtered in the repository.</summary>
public sealed class ListProductsQueryHandler(
    ICurrentUser currentUser,
    IProductRepository products) : IRequestHandler<ListProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.ShopId))
            throw new UnauthorizedAppException("Not authenticated.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var result = await products.ListAsync(
            currentUser.ShopId,
            request.Search,
            request.Category,
            page,
            pageSize,
            cancellationToken);

        return new PagedResult<ProductDto>
        {
            Items = result.Items.Select(ProductMapping.ToDto).ToList(),
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
