using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Shared.DTOs.Products;

/// <summary>Manual stock adjustment. <see cref="QuantityDelta"/> must be non-zero; resulting stock cannot go below zero.</summary>
public class AdjustStockRequest
{
    /// <summary>Signed quantity (positive inbound, negative outbound).</summary>
    [Required]
    public int QuantityDelta { get; set; }

    /// <summary>Must match the product's current version or the API returns 409 concurrency.</summary>
    [Required]
    [Range(1, long.MaxValue)]
    public long ExpectedVersion { get; set; }

    [StringLength(400)]
    public string? Notes { get; set; }
}
