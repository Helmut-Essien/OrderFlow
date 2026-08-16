using OrderFlow.Domain.Entities;
using OrderFlow.Shared.DTOs.Dashboard;
using OrderFlow.Shared.DTOs.Products;

namespace OrderFlow.Application.Common.Interfaces;

/// <summary>Result of an atomic stock UPDATE (new on-hand quantity and concurrency token).</summary>
public sealed record StockAdjustmentResult(int Stock, long Version);

/// <summary>Page of product DTOs plus the unpaged total for the shop/filter. Items are projected in SQL.</summary>
public sealed record ProductListResult(IReadOnlyList<ProductDto> Items, int TotalCount);

/// <summary>
/// Persistence port for shop-scoped products. Implementations must honor EF global <c>ShopId</c> filters.
/// </summary>
public interface IProductRepository
{
    /// <summary>Untracked read for GET/list mapping. Do not mutate the result.</summary>
    Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Tracked load for catalog updates so <c>SaveChanges</c> persists <c>UpdateDetails</c>.</summary>
    Task<Product?> GetTrackedByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Untracked lookup by shop and already-normalized (uppercase) SKU.</summary>
    Task<Product?> GetBySkuAsync(string shopId, string sku, CancellationToken cancellationToken = default);

    /// <summary>Paged catalog. Projects to <see cref="ProductDto"/> in SQL (no full entity materialize).</summary>
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

    /// <summary>Active low-stock rows projected to dashboard DTOs (capped at 50).</summary>
    Task<IReadOnlyList<LowStockItemDto>> GetLowStockAsync(string shopId, CancellationToken cancellationToken = default);

    /// <summary>Stages a new product for insert. Call <see cref="IUnitOfWork.SaveChangesAsync"/> to persist.</summary>
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
