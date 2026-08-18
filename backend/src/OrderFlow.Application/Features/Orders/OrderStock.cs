using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Application.Features.Orders;

/// <summary>
/// Applies reserve/release via the same atomic SQL UPDATE as manual stock adjust.
/// Paid writes a Deduct movement only — on-hand already fell at Confirmed.
/// </summary>
internal static class OrderStock
{
    /// <summary>Decrements on-hand for each line (Reserve). Fails the whole transaction on the first insufficient or stale row.</summary>
    public static Task ReserveAsync(
        IProductRepository products,
        IStockMovementRepository movements,
        IUnitOfWork unitOfWork,
        Order order,
        string shopId,
        string? userId,
        CancellationToken cancellationToken)
    {
        return ApplyDeltasAsync(
            products,
            movements,
            unitOfWork,
            order,
            shopId,
            userId,
            quantitySign: -1,
            StockMovementType.Reserve,
            cancellationToken);
    }

    /// <summary>Returns reserved qty to on-hand (Release) when cancelling a Confirmed or Paid order.</summary>
    public static Task ReleaseAsync(
        IProductRepository products,
        IStockMovementRepository movements,
        IUnitOfWork unitOfWork,
        Order order,
        string shopId,
        string? userId,
        CancellationToken cancellationToken)
    {
        return ApplyDeltasAsync(
            products,
            movements,
            unitOfWork,
            order,
            shopId,
            userId,
            quantitySign: 1,
            StockMovementType.Release,
            cancellationToken);
    }

    /// <summary>
    /// Audit-only Deduct rows. Stock was already held by Reserve; no physical stock change occurs.
    /// <c>QuantityDelta</c> is zero and <c>ResultingStock</c> is a point-in-time snapshot.
    /// </summary>
    public static async Task WriteDeductAuditAsync(
        IProductRepository products,
        IStockMovementRepository movements,
        Order order,
        string shopId,
        string? userId,
        CancellationToken cancellationToken)
    {
        var ids = order.Lines.Select(l => l.ProductId).Distinct(StringComparer.Ordinal).ToList();
        var freshProducts = await products.GetByIdsAsync(ids, cancellationToken);
        var byId = freshProducts.ToDictionary(p => p.Id, StringComparer.Ordinal);

        foreach (var line in order.Lines)
        {
            if (!byId.TryGetValue(line.ProductId, out var product))
                throw new NotFoundAppException("Product not found.");

            // Delta is 0: stock was already decremented by Reserve. This row is purely for audit.
            movements.Add(StockMovement.Create(
                shopId,
                product.Id,
                0,
                product.Stock,
                StockMovementType.Deduct,
                $"Paid order {order.Id} ({line.Quantity} × {product.Sku})",
                userId));
        }
    }

    private static async Task ApplyDeltasAsync(
        IProductRepository products,
        IStockMovementRepository movements,
        IUnitOfWork unitOfWork,
        Order order,
        string shopId,
        string? userId,
        int quantitySign,
        StockMovementType type,
        CancellationToken cancellationToken)
    {
        if (!unitOfWork.IsInTransaction)
            throw new InvalidOperationException("Stock mutations must run inside a database transaction.");

        var loaded = await LoadLinesAsync(products, order, cancellationToken);
        foreach (var (line, product) in loaded)
        {
            if (quantitySign < 0 && !product.IsActive)
                throw new ConflictAppException($"{product.Sku} is inactive and cannot be sold.");

            var delta = quantitySign * line.Quantity;
            var adjusted = await TryAdjustWithRetryAsync(
                products, product, shopId, delta, cancellationToken);

            movements.Add(StockMovement.Create(
                shopId,
                product.Id,
                delta,
                adjusted.Stock,
                type,
                $"{type} for order {order.Id}",
                userId));
        }
    }

    /// <summary>
    /// Retries the atomic stock UPDATE up to 2 times when the failure is a pure version mismatch
    /// (another concurrent order bumped the version but stock is still sufficient).
    /// </summary>
    private static async Task<StockAdjustmentResult> TryAdjustWithRetryAsync(
        IProductRepository products,
        Product product,
        string shopId,
        int delta,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;
        var version = product.Version;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var adjusted = await products.TryAdjustStockAsync(
                product.Id, shopId, version, delta, cancellationToken);

            if (adjusted is not null)
                return adjusted;

            var existing = await products.GetByIdAsync(product.Id, cancellationToken)
                ?? throw new NotFoundAppException("Product not found.");

            var resulting = existing.Stock + delta;
            if (resulting < 0)
                throw new ConflictAppException($"Not enough stock for {existing.Sku}.");

            if (resulting > ProductConstraints.MaxStock)
                throw new ConflictAppException($"Stock cannot exceed {ProductConstraints.MaxStock:N0}.");

            if (attempt == maxAttempts)
                throw new ConcurrencyAppException("Stock changed while this order was saving. Refresh and try again.");

            // Version mismatch but stock is sufficient — retry with the fresh version.
            version = existing.Version;
        }

        throw new ConcurrencyAppException("Stock changed while this order was saving. Refresh and try again.");
    }

    private static async Task<List<(OrderLine Line, Product Product)>> LoadLinesAsync(
        IProductRepository products,
        Order order,
        CancellationToken cancellationToken)
    {
        var ids = order.Lines.Select(l => l.ProductId).Distinct(StringComparer.Ordinal).ToList();
        var loaded = await products.GetByIdsAsync(ids, cancellationToken);
        var byId = loaded.ToDictionary(p => p.Id, StringComparer.Ordinal);

        var pairs = new List<(OrderLine, Product)>(order.Lines.Count);
        foreach (var line in order.Lines)
        {
            if (!byId.TryGetValue(line.ProductId, out var product))
                throw new NotFoundAppException("Product not found.");

            pairs.Add((line, product));
        }

        return pairs;
    }
}
