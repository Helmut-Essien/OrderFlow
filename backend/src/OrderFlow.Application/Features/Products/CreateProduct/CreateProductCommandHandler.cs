using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Application.Features.Products.CreateProduct;

/// <summary>
/// Creates a product in the authenticated shop, enforcing plan product caps and SKU uniqueness.
/// </summary>
/// <exception cref="UnauthorizedAppException">JWT is missing a shop id.</exception>
/// <exception cref="ForbiddenAppException">Shop is at <c>PlanQuota.MaxProducts</c>.</exception>
/// <exception cref="ConflictAppException">SKU already exists in the shop.</exception>
public sealed class CreateProductCommandHandler(
    ICurrentUser currentUser,
    IShopRepository shops,
    IProductRepository products,
    IStockMovementRepository stockMovements,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateProductCommand, ProductDto>
{
    /// <summary>
    /// Persists the product and an opening <see cref="StockMovement"/> when initial stock is non-zero.
    /// Plan cap and insert share one transaction so concurrent creates cannot exceed <c>MaxProducts</c>.
    /// </summary>
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var shopId = RequireShopId(currentUser);
        ProductDto? dto = null;

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await shops.AcquirePlanCapLockAsync(shopId, ct);

            var shop = await shops.GetByIdAsync(shopId, ct)
                ?? throw new NotFoundAppException("Shop not found.");

            // Plan caps live here (not Domain) so quota can change with Platform planName.
            var quota = PlanQuota.FromPlanName(shop.PlanName);
            if (quota.MaxProducts is int maxProducts)
            {
                var count = await products.CountByShopAsync(shopId, ct);
                if (count >= maxProducts)
                    throw new ForbiddenAppException($"Your {quota.Name} plan allows up to {maxProducts} products.");
            }

            var sku = Product.NormalizeSku(request.Sku);
            if (await products.GetBySkuAsync(shopId, sku, ct) is not null)
                throw new ConflictAppException("A product with this SKU already exists.");

            var product = Product.Create(
                shopId,
                request.Name,
                sku,
                request.Category,
                request.Price,
                request.Stock,
                request.LowStockThreshold);

            products.Add(product);

            if (product.Stock != 0)
            {
                stockMovements.Add(StockMovement.Create(
                    shopId,
                    product.Id,
                    product.Stock,
                    product.Stock,
                    StockMovementType.Adjustment,
                    "Opening stock",
                    currentUser.UserId));
            }

            await unitOfWork.SaveChangesAsync(ct);
            dto = ProductMapping.ToDto(product);
        }, cancellationToken);

        return dto!;
    }

    private static string RequireShopId(ICurrentUser currentUser)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.ShopId))
            throw new UnauthorizedAppException("Not authenticated.");

        return currentUser.ShopId;
    }
}
