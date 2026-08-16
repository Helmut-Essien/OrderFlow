using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Entities;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Application.Features.Products.UpdateProduct;

/// <summary>
/// Updates product details in the current shop. Bumps <c>Version</c> so concurrent stock writes fail.
/// </summary>
/// <exception cref="ConcurrencyAppException">Client sent a stale <c>expectedVersion</c>.</exception>
/// <exception cref="ConflictAppException">New SKU belongs to another product in the shop.</exception>
public sealed class UpdateProductCommandHandler(
    ICurrentUser currentUser,
    IProductRepository products,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateProductCommand, ProductDto>
{
    /// <summary>Applies details and bumps <c>Version</c>. Stock is unchanged; use adjust-stock for quantity.</summary>
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.ShopId))
            throw new UnauthorizedAppException("Not authenticated.");

        var product = await products.GetTrackedByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundAppException("Product not found.");

        if (product.Version != request.ExpectedVersion)
            throw new ConcurrencyAppException("This product was updated by someone else. Refresh and try again.");

        var sku = Product.NormalizeSku(request.Sku);
        var existing = await products.GetBySkuAsync(currentUser.ShopId, sku, cancellationToken);
        if (existing is not null && existing.Id != product.Id)
            throw new ConflictAppException("A product with this SKU already exists.");

        product.UpdateDetails(
            request.Name,
            sku,
            request.Category,
            request.Price,
            request.LowStockThreshold,
            request.IsActive);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductMapping.ToDto(product);
    }
}
