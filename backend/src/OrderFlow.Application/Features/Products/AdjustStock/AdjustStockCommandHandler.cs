using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Application.Features.Products.AdjustStock;

/// <summary>
/// Applies a manual stock delta with an atomic SQL update, then writes an Adjustment <see cref="StockMovement"/> in the same transaction.
/// </summary>
/// <exception cref="ConcurrencyAppException">Stale <c>expectedVersion</c>.</exception>
/// <exception cref="ConflictAppException">Delta would take stock below zero or above <see cref="ProductConstraints.MaxStock"/>.</exception>
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
        ProductDto? dto = null;

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // Atomic UPDATE (version + stock range). Do not replace with read-modify-write.
            var adjusted = await products.TryAdjustStockAsync(
                request.ProductId,
                shopId,
                request.ExpectedVersion,
                request.QuantityDelta,
                ct);

            if (adjusted is null)
            {
                var existing = await products.GetByIdAsync(request.ProductId, ct)
                    ?? throw new NotFoundAppException("Product not found.");

                if (existing.Version != request.ExpectedVersion)
                    throw new ConcurrencyAppException("This product was updated by someone else. Refresh and try again.");

                var resulting = existing.Stock + request.QuantityDelta;
                if (resulting < 0)
                    throw new ConflictAppException("Stock cannot go below zero.");

                if (resulting > ProductConstraints.MaxStock)
                    throw new ConflictAppException($"Stock cannot exceed {ProductConstraints.MaxStock:N0}.");

                throw new ConcurrencyAppException("This product was updated by someone else. Refresh and try again.");
            }

            var product = await products.GetByIdAsync(request.ProductId, ct)
                ?? throw new NotFoundAppException("Product not found.");

            // Same-transaction reload can lag the SQL UPDATE; apply the returned stock before mapping.
            if (product.Version < adjusted.Version)
                product.ApplyStock(adjusted.Stock, adjusted.Version);

            stockMovements.Add(StockMovement.Create(
                shopId,
                product.Id,
                request.QuantityDelta,
                adjusted.Stock,
                StockMovementType.Adjustment,
                request.Notes,
                currentUser.UserId));

            await unitOfWork.SaveChangesAsync(ct);
            dto = ProductMapping.ToDto(product);
        }, cancellationToken);

        return dto!;
    }
}
