using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Enums;
using OrderFlow.Domain.Entities;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Application.Features.Products.AdjustStock;

/// <summary>
/// Applies a manual stock delta with an atomic SQL update, then writes an Adjustment <see cref="StockMovement"/>.
/// </summary>
/// <exception cref="ConcurrencyAppException">Stale <c>expectedVersion</c>.</exception>
/// <exception cref="ConflictAppException">Delta would take stock below zero.</exception>
public sealed class AdjustStockCommandHandler(
    ICurrentUser currentUser,
    IProductRepository products,
    IStockMovementRepository stockMovements,
    IUnitOfWork unitOfWork) : IRequestHandler<AdjustStockCommand, ProductDto>
{
    public async Task<ProductDto> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.ShopId))
            throw new UnauthorizedAppException("Not authenticated.");

        var shopId = currentUser.ShopId;
        // Atomic UPDATE (version + stock range). Do not replace with read-modify-write.
        var adjusted = await products.TryAdjustStockAsync(
            request.ProductId,
            shopId,
            request.ExpectedVersion,
            request.QuantityDelta,
            cancellationToken);

        if (adjusted is null)
        {
            var existing = await products.GetByIdAsync(request.ProductId, cancellationToken)
                ?? throw new NotFoundAppException("Product not found.");

            if (existing.Version != request.ExpectedVersion)
                throw new ConcurrencyAppException("This product was updated by someone else. Refresh and try again.");

            throw new ConflictAppException("Stock cannot go below zero.");
        }

        var product = await products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundAppException("Product not found.");

        stockMovements.Add(StockMovement.Create(
            shopId,
            product.Id,
            request.QuantityDelta,
            adjusted.Stock,
            StockMovementType.Adjustment,
            request.Notes,
            currentUser.UserId));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductMapping.ToDto(product);
    }
}
