namespace OrderFlow.Shared.DTOs.Products;

/// <summary>Public product contract. <see cref="Version"/> is the optimistic concurrency token for updates and stock.</summary>
public class ProductDto
{
    public required string Id { get; set; }

    public required string ShopId { get; set; }

    public required string Name { get; set; }

    /// <summary>Uppercase SKU unique within the shop.</summary>
    public required string Sku { get; set; }

    public string? Category { get; set; }

    /// <summary>Unit price in GHS.</summary>
    public required decimal Price { get; set; }

    public required int Stock { get; set; }

    public required int LowStockThreshold { get; set; }

    public required bool IsActive { get; set; }

    /// <summary>Computed: <c>Stock &lt;= LowStockThreshold</c>. Not stored.</summary>
    public required bool IsLowStock { get; set; }

    /// <summary>Send back as <c>expectedVersion</c> on PUT and stock adjust.</summary>
    public required long Version { get; set; }

    public required DateTime CreatedAt { get; set; }

    public required DateTime UpdatedAt { get; set; }
}
