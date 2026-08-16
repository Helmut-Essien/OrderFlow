using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Shared.DTOs.Products;

/// <summary>Create-product body. Lengths match <c>ProductConstraints</c> and Angular <c>PRODUCT_FIELD_LIMITS</c>.</summary>
public class CreateProductRequest
{
    /// <summary>Display name, max 200.</summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>SKU; API stores it uppercase, unique per shop, max 50.</summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Sku { get; set; } = string.Empty;

    /// <summary>Optional grouping label, max 80.</summary>
    [StringLength(80)]
    public string? Category { get; set; }

    /// <summary>Unit price in GHS (0–999,999,999.99).</summary>
    [Required]
    [Range(0, 999_999_999.99)]
    public decimal Price { get; set; }

    /// <summary>Opening on-hand quantity (0–99,999,999).</summary>
    [Range(0, 99_999_999)]
    public int Stock { get; set; }

    /// <summary>Dashboard flags the SKU when stock is at or below this value.</summary>
    [Range(0, 99_999_999)]
    public int LowStockThreshold { get; set; }
}
