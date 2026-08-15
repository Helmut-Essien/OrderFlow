using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Common.Interfaces;

/// <summary>Result of an atomic stock UPDATE (new on-hand quantity and concurrency token).</summary>
public sealed record StockAdjustmentResult(int Stock, long Version);

/// <summary>Page of products plus the unpaged total for the shop/filter.</summary>
public sealed record ProductListResult(IReadOnlyList<Product> Items, int TotalCount);

/// <summary>
/// Persistence port for shop-scoped products. Implementations must honor EF global <c>ShopId</c> filters.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Looks up by shop and already-normalized (uppercase) SKU.</summary>
    Task<Product?> GetBySkuAsync(string shopId, string sku, CancellationToken cancellationToken = default);

    Task<ProductListResult> ListAsync(
        string shopId,
        string? search,
        string? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Active product count used to enforce <c>PlanQuota.MaxProducts</c>. Inactive SKUs do not consume a slot.</summary>
    Task<int> CountByShopAsync(string shopId, CancellationToken cancellationToken = default);

    /// <summary>Distinct non-empty categories for the shop (all products, not just the current page).</summary>
    Task<IReadOnlyList<string>> ListCategoriesAsync(string shopId, CancellationToken cancellationToken = default);

    /// <summary>Active products where stock is at or below the low-stock threshold (capped for dashboard).</summary>
    Task<IReadOnlyList<Product>> GetLowStockAsync(string shopId, CancellationToken cancellationToken = default);

    void Add(Product product);

    /// <summary>
    /// Atomically applies <paramref name="quantityDelta"/> when <c>Version</c> matches and resulting stock stays in range.
    /// Returns null when no row was updated (stale version, insufficient stock, or overflow).
    /// Call inside <see cref="IUnitOfWork.ExecuteInTransactionAsync"/> when also inserting a <c>StockMovement</c>.
    /// </summary>
    Task<StockAdjustmentResult?> TryAdjustStockAsync(
        string productId,
        string shopId,
        long expectedVersion,
        int quantityDelta,
        CancellationToken cancellationToken = default);
}
