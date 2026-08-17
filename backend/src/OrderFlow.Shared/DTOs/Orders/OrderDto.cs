namespace OrderFlow.Shared.DTOs.Orders;

/// <summary>Public order contract including line snapshots. <see cref="Version"/> is the optimistic concurrency token for status changes.</summary>
public class OrderDto
{
    /// <summary>Order ULID.</summary>
    public required string Id { get; set; }

    /// <summary>Tenant shop that owns this order.</summary>
    public required string ShopId { get; set; }

    /// <summary>Customer display name.</summary>
    public required string CustomerName { get; set; }

    /// <summary>Optional customer phone.</summary>
    public string? CustomerPhone { get; set; }

    /// <summary>Optional shop notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Lifecycle status name (<c>Pending</c>, <c>Confirmed</c>, <c>Paid</c>, <c>Fulfilled</c>, <c>Cancelled</c>).</summary>
    public required string Status { get; set; }

    /// <summary>Entry channel name (<c>Manual</c> or <c>WhatsApp</c>).</summary>
    public required string Source { get; set; }

    /// <summary>WhatsApp unmatched-text flag. Always false for manual orders in this slice.</summary>
    public required bool NeedsClarification { get; set; }

    /// <summary>Sum of line totals in GHS.</summary>
    public required decimal TotalAmount { get; set; }

    /// <summary>Send back as <c>expectedVersion</c> on status changes.</summary>
    public required long Version { get; set; }

    /// <summary>UTC insert time.</summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>UTC last status mutation.</summary>
    public required DateTime UpdatedAt { get; set; }

    /// <summary>UTC instant when the order became Confirmed, if ever.</summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>UTC instant when the order became Paid. Dashboard “today” sales use this.</summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>UTC instant when the order became Fulfilled, if ever.</summary>
    public DateTime? FulfilledAt { get; set; }

    /// <summary>UTC instant when the order became Cancelled, if ever.</summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>Line snapshots in create order.</summary>
    public required IReadOnlyList<OrderLineDto> Lines { get; set; }
}

/// <summary>One SKU snapshot on an order. Catalog edits after create do not change these fields.</summary>
public class OrderLineDto
{
    /// <summary>Line ULID.</summary>
    public required string Id { get; set; }

    /// <summary>Catalog product ULID at create time.</summary>
    public required string ProductId { get; set; }

    /// <summary>Product name at create time.</summary>
    public required string ProductName { get; set; }

    /// <summary>Uppercase SKU at create time.</summary>
    public required string Sku { get; set; }

    /// <summary>Units ordered.</summary>
    public required int Quantity { get; set; }

    /// <summary>Unit price in GHS at create time.</summary>
    public required decimal UnitPrice { get; set; }

    /// <summary>Line total in GHS.</summary>
    public required decimal LineTotal { get; set; }
}
