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
        Order order,
        string shopId,
        string? userId,
        CancellationToken cancellationToken)
    {
        return ApplyDeltasAsync(
            products,
            movements,
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
        Order order,
        string shopId,
        string? userId,
        CancellationToken cancellationToken)
    {
        return ApplyDeltasAsync(
            products,
            movements,
            order,
            shopId,
            userId,
            quantitySign: 1,
            StockMovementType.Release,
            cancellationToken);
    }

    /// <summary>
    /// Audit-only Deduct rows. Stock was already reserved; a second decrement would oversell.
    /// </summary>
    public static async Task WriteDeductAuditAsync(
        IProductRepository products,
        IStockMovementRepository movements,
        Order order,
        string shopId,
        string? userId,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadLinesAsync(products, order, cancellationToken);
        foreach (var (line, product) in loaded)
        {
            movements.Add(StockMovement.Create(
                shopId,
                product.Id,
                -line.Quantity,
                product.Stock,
                StockMovementType.Deduct,
                $"Paid order {order.Id}",
                userId));
        }
    }

    private static async Task ApplyDeltasAsync(
        IProductRepository products,
        IStockMovementRepository movements,
        Order order,
        string shopId,
        string? userId,
        int quantitySign,
        StockMovementType type,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadLinesAsync(products, order, cancellationToken);
        foreach (var (line, product) in loaded)
        {
            // Inactive SKUs must not be newly reserved; cancelling must still return stock.
            if (quantitySign < 0 && !product.IsActive)
                throw new ConflictAppException($"{product.Sku} is inactive and cannot be sold.");

            var delta = quantitySign * line.Quantity;
            var adjusted = await products.TryAdjustStockAsync(
                product.Id,
                shopId,
                product.Version,
                delta,
                cancellationToken);

            if (adjusted is null)
            {
                var existing = await products.GetByIdAsync(product.Id, cancellationToken)
                    ?? throw new NotFoundAppException("Product not found.");

                if (existing.Version != product.Version)
                    throw new ConcurrencyAppException("Stock changed while this order was saving. Refresh and try again.");

                var resulting = existing.Stock + delta;
                if (resulting < 0)
                    throw new ConflictAppException($"Not enough stock for {existing.Sku}.");

                if (resulting > ProductConstraints.MaxStock)
                    throw new ConflictAppException($"Stock cannot exceed {ProductConstraints.MaxStock:N0}.");

                throw new ConcurrencyAppException("Stock changed while this order was saving. Refresh and try again.");
            }

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
