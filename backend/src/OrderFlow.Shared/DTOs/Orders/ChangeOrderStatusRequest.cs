using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Shared.DTOs.Orders;

/// <summary>Status-change body. <see cref="Status"/> must be a defined <c>OrderStatus</c> name (e.g. Confirmed).</summary>
public class ChangeOrderStatusRequest
{
    /// <summary>Target status: Confirmed, Paid, Fulfilled, or Cancelled.</summary>
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string Status { get; set; } = string.Empty;

    /// <summary>Send back the order <c>version</c> from GET so concurrent transitions 409.</summary>
    [Range(1, long.MaxValue)]
    public long ExpectedVersion { get; set; }
}
