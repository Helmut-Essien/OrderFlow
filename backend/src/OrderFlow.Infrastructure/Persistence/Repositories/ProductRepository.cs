using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Product persistence. Stock changes go through <see cref="TryAdjustStockAsync"/> (atomic SQL), not a tracked entity update.
/// List/get/dashboard reads are untracked; catalog updates use <see cref="GetTrackedByIdAsync"/>.
/// </summary>
public sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public Task<Product?> GetTrackedByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public Task<Product?> GetBySkuAsync(string shopId, string sku, CancellationToken cancellationToken = default)
    {
        return db.Products.AsNoTracking().FirstOrDefaultAsync(
            p => p.ShopId == shopId && p.Sku == sku,
            cancellationToken);
    }

    public async Task<ProductListResult> ListAsync(
        string shopId,
        string? search,
        string? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = db.Products.AsNoTracking().Where(p => p.ShopId == shopId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ILike keeps the expression sargable-friendly vs ToLower().Contains, which cannot use indexes.
            var pattern = ToILikeContainsPattern(search);
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, pattern, "\\") ||
                EF.Functions.ILike(p.Sku, pattern, "\\"));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = category.Trim();
            query = query.Where(p => p.Category == normalizedCategory);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Sku)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new ProductListResult(items, total);
    }

    public Task<int> CountByShopAsync(string shopId, CancellationToken cancellationToken = default)
    {
        return db.Products.CountAsync(p => p.ShopId == shopId && p.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListCategoriesAsync(
        string shopId,
        CancellationToken cancellationToken = default)
    {
        return await db.Products
            .AsNoTracking()
            .Where(p => p.ShopId == shopId && p.Category != null)
            .Select(p => p.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetLowStockAsync(
        string shopId,
        CancellationToken cancellationToken = default)
    {
        return await db.Products
            .AsNoTracking()
            .Where(p => p.ShopId == shopId && p.IsActive && p.Stock <= p.LowStockThreshold)
            .OrderBy(p => p.Stock)
            .ThenBy(p => p.Name)
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public void Add(Product product) => db.Products.Add(product);

    public async Task<StockAdjustmentResult?> TryAdjustStockAsync(
        string productId,
        string shopId,
        long expectedVersion,
        int quantityDelta,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var maxStock = ProductConstraints.MaxStock;

        // Single-statement UPDATE: version match + resulting stock in range. Zero rows → concurrency or insufficient stock.
        var rows = await db.Database
            .SqlQuery<StockAdjustRow>($"""
                UPDATE "Products"
                SET "Stock" = "Stock" + {quantityDelta},
                    "Version" = "Version" + 1,
                    "UpdatedAt" = {now}
                WHERE "Id" = {productId}
                  AND "ShopId" = {shopId}
                  AND "Version" = {expectedVersion}
                  AND "Stock" + {quantityDelta} >= 0
                  AND "Stock" + {quantityDelta} <= {maxStock}
                RETURNING "Stock", "Version"
                """)
            .ToListAsync(cancellationToken);

        var row = rows.FirstOrDefault();
        if (row is null)
            return null;

        var tracked = db.Products.Local.FirstOrDefault(p => p.Id == productId);
        if (tracked is not null)
            db.Entry(tracked).State = EntityState.Detached;

        return new StockAdjustmentResult(row.Stock, row.Version);
    }

    /// <summary>Wraps user search in <c>%...%</c> and escapes LIKE wildcards so they are literal.</summary>
    private static string ToILikeContainsPattern(string search)
    {
        var escaped = search.Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }

    private sealed class StockAdjustRow
    {
        public int Stock { get; set; }

        public long Version { get; set; }
    }
}
