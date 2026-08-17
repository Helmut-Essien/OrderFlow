using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Shared.DTOs.Orders;

/// <summary>Create-order body. Lengths match <c>OrderConstraints</c>. Unit prices are taken from the catalog, not this payload.</summary>
public class CreateOrderRequest
{
    /// <summary>Customer display name, max 200.</summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>Optional customer phone, max 50.</summary>
    [StringLength(50)]
    public string? CustomerPhone { get; set; }

    /// <summary>Optional shop notes, max 400.</summary>
    [StringLength(400)]
    public string? Notes { get; set; }

    /// <summary>
    /// When true, the order is created already Confirmed and stock is reserved in the same transaction.
    /// When false, the order stays Pending and does not touch stock.
    /// </summary>
    public bool ConfirmImmediately { get; set; }

    /// <summary>One row per product. Duplicate product ids are rejected. Max 50 lines.</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(50)]
    public List<CreateOrderLineRequest> Lines { get; set; } = [];
}

/// <summary>One SKU on a new order. Quantity only — price is snapshotted from the product.</summary>
public class CreateOrderLineRequest
{
    /// <summary>Catalog product ULID.</summary>
    [Required]
    [StringLength(26, MinimumLength = 1)]
    public string ProductId { get; set; } = string.Empty;

    /// <summary>Units to sell (1–99,999,999).</summary>
    [Range(1, 99_999_999)]
    public int Quantity { get; set; }
}
