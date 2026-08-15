using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Shared.DTOs.Products;

/// <summary>Create-product body. Lengths match <c>ProductConstraints</c> and Angular <c>PRODUCT_FIELD_LIMITS</c>.</summary>
public class CreateProductRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Sku { get; set; } = string.Empty;

    [StringLength(80)]
    public string? Category { get; set; }

    [Required]
    [Range(0, 999_999_999.99)]
    public decimal Price { get; set; }

    [Range(0, 99_999_999)]
    public int Stock { get; set; }

    [Range(0, 99_999_999)]
    public int LowStockThreshold { get; set; }
}
