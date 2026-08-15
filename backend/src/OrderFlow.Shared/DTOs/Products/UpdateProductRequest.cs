using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Shared.DTOs.Products;

/// <summary>Update-product body. Does not include stock; send <see cref="ExpectedVersion"/> from the last <see cref="ProductDto"/>.</summary>
public class UpdateProductRequest
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
    public int LowStockThreshold { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Must match the product's current <see cref="ProductDto.Version"/> or the API returns 409 concurrency.</summary>
    [Required]
    [Range(1, long.MaxValue)]
    public long ExpectedVersion { get; set; }
}
