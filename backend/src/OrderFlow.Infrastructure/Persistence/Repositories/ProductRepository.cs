using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Product persistence. Stock changes go through <see cref="TryAdjustStockAsync"/> (atomic SQL), not a tracked entity update.
/// </summary>
public sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public Task<Product?> GetBySkuAsync(string shopId, string sku, CancellationToken cancellationToken = default)
    {
        return db.Products.FirstOrDefaultAsync(
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
        var query = db.Products.AsQueryable().Where(p => p.ShopId == shopId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Sku.ToLower().Contains(term));
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

    private sealed class StockAdjustRow
    {
        public int Stock { get; set; }

        public long Version { get; set; }
    }
}
