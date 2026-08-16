namespace OrderFlow.Shared.DTOs.Products;

/// <summary>Public product contract. <see cref="Version"/> is the optimistic concurrency token for updates and stock.</summary>
public class ProductDto
{
    /// <summary>Product ULID.</summary>
    public required string Id { get; set; }

    /// <summary>Tenant shop that owns this product.</summary>
    public required string ShopId { get; set; }

    /// <summary>Display name.</summary>
    public required string Name { get; set; }

    /// <summary>Uppercase SKU unique within the shop.</summary>
    public required string Sku { get; set; }

    /// <summary>Optional grouping label.</summary>
    public string? Category { get; set; }

    /// <summary>Unit price in GHS.</summary>
    public required decimal Price { get; set; }

    /// <summary>On-hand quantity.</summary>
    public required int Stock { get; set; }

    /// <summary>Dashboard flags the SKU when <see cref="Stock"/> is at or below this value.</summary>
    public required int LowStockThreshold { get; set; }

    /// <summary>Inactive SKUs do not count toward the plan product cap.</summary>
    public required bool IsActive { get; set; }

    /// <summary>Computed: <c>Stock &lt;= LowStockThreshold</c>. Not stored.</summary>
    public required bool IsLowStock { get; set; }

    /// <summary>Send back as <c>expectedVersion</c> on PUT and stock adjust.</summary>
    public required long Version { get; set; }

    /// <summary>UTC insert time.</summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>UTC last mutation time.</summary>
    public required DateTime UpdatedAt { get; set; }
}
