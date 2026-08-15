using OrderFlow.Domain;

namespace OrderFlow.Domain.Entities;

/// <summary>
/// Shop-scoped catalog item with optimistic-concurrency stock. SKU is unique per shop and stored uppercase.
/// </summary>
public class Product
{
    public string Id { get; private set; } = NUlid.Ulid.NewUlid().ToString();

    /// <summary>Tenant shop that owns this product.</summary>
    public string ShopId { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    /// <summary>Uppercase SKU, unique within the shop (max 50).</summary>
    public string Sku { get; private set; } = string.Empty;

    public string? Category { get; private set; }

    /// <summary>Unit price in GHS, rounded to 2 decimal places (0–999,999,999.99).</summary>
    public decimal Price { get; private set; }

    /// <summary>On-hand quantity. Mutate only via <see cref="ApplyStock"/> after an atomic SQL update.</summary>
    public int Stock { get; private set; }

    public int LowStockThreshold { get; private set; }

    public bool IsActive { get; private set; } = true;

    /// <summary>Optimistic concurrency token. Starts at 1; increment on every details or stock change.</summary>
    public long Version { get; private set; } = 1;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public Shop Shop { get; private set; } = null!;

    public ICollection<StockMovement> StockMovements { get; private set; } = [];

    private Product()
    {
    }

    /// <summary>True when on-hand stock is at or below the low-stock threshold. Not persisted.</summary>
    public bool IsLowStock => Stock <= LowStockThreshold;

    /// <summary>
    /// Creates a product with normalized name/SKU/category and rounded price. Does not enforce plan caps or SKU uniqueness.
    /// </summary>
    public static Product Create(
        string shopId,
        string name,
        string sku,
        string? category,
        decimal price,
        int stock,
        int lowStockThreshold)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shopId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);

        var normalizedName = NormalizeRequired(name, ProductConstraints.NameMaxLength, nameof(name));
        var normalizedSku = NormalizeSku(sku);
        var normalizedCategory = NormalizeOptional(category, ProductConstraints.CategoryMaxLength, nameof(category));

        if (price < 0 || price > ProductConstraints.MaxPrice)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be between 0 and 999,999,999.99.");

        if (stock < 0 || stock > ProductConstraints.MaxStock)
            throw new ArgumentOutOfRangeException(nameof(stock), "Stock must be between 0 and 99,999,999.");

        if (lowStockThreshold < 0 || lowStockThreshold > ProductConstraints.MaxStock)
            throw new ArgumentOutOfRangeException(nameof(lowStockThreshold), "Low-stock threshold must be between 0 and 99,999,999.");

        return new Product
        {
            ShopId = shopId.Trim(),
            Name = normalizedName,
            Sku = normalizedSku,
            Category = normalizedCategory,
            Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero),
            Stock = stock,
            LowStockThreshold = lowStockThreshold,
            IsActive = true,
            Version = 1
        };
    }

    /// <summary>
    /// Updates catalog fields. Does not change stock; bumps <see cref="Version"/> so concurrent stock writes fail.
    /// </summary>
    public void UpdateDetails(
        string name,
        string sku,
        string? category,
        decimal price,
        int lowStockThreshold,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);

        Name = NormalizeRequired(name, ProductConstraints.NameMaxLength, nameof(name));
        Sku = NormalizeSku(sku);
        Category = NormalizeOptional(category, ProductConstraints.CategoryMaxLength, nameof(category));

        if (price < 0 || price > ProductConstraints.MaxPrice)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be between 0 and 999,999,999.99.");

        if (lowStockThreshold < 0 || lowStockThreshold > ProductConstraints.MaxStock)
            throw new ArgumentOutOfRangeException(nameof(lowStockThreshold), "Low-stock threshold must be between 0 and 99,999,999.");

        Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero);
        LowStockThreshold = lowStockThreshold;
        IsActive = isActive;
        Version += 1;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Applies the result of an atomic stock UPDATE. Callers must already have incremented version in SQL.
    /// </summary>
    public void ApplyStock(int newStock, long newVersion)
    {
        if (newStock < 0 || newStock > ProductConstraints.MaxStock)
            throw new ArgumentOutOfRangeException(nameof(newStock), "Stock must be between 0 and 99,999,999.");

        if (newVersion <= Version)
            throw new ArgumentOutOfRangeException(nameof(newVersion), "Version must increase.");

        Stock = newStock;
        Version = newVersion;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Trims and uppercases SKU; rejects values longer than <see cref="ProductConstraints.SkuMaxLength"/>.</summary>
    public static string NormalizeSku(string sku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        var normalized = sku.Trim().ToUpperInvariant();
        if (normalized.Length > ProductConstraints.SkuMaxLength)
            throw new ArgumentOutOfRangeException(nameof(sku), $"SKU cannot exceed {ProductConstraints.SkuMaxLength} characters.");
        return normalized;
    }

    private static string NormalizeRequired(string value, int maxLength, string paramName)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("Value cannot be empty.", paramName);
        if (trimmed.Length > maxLength)
            throw new ArgumentOutOfRangeException(paramName, $"Value cannot exceed {maxLength} characters.");
        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentOutOfRangeException(paramName, $"Value cannot exceed {maxLength} characters.");

        return trimmed;
    }
}
