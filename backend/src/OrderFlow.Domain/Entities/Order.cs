using OrderFlow.Domain.Enums;

namespace OrderFlow.Domain.Entities;

/// <summary>
/// Shop-scoped sales order. Stock is reserved on <see cref="OrderStatus.Confirmed"/>, audited as deducted on Paid, and released on Cancelled.
/// </summary>
public class Order
{
    /// <summary>ULID primary key.</summary>
    public string Id { get; private set; } = NUlid.Ulid.NewUlid().ToString();

    /// <summary>Tenant shop that owns this order.</summary>
    public string ShopId { get; private set; } = string.Empty;

    /// <summary>Customer display name, max 200 characters. Not a Platform identity.</summary>
    public string CustomerName { get; private set; } = string.Empty;

    /// <summary>Optional customer phone, max 50 characters.</summary>
    public string? CustomerPhone { get; private set; }

    /// <summary>Optional shop notes, max 400 characters.</summary>
    public string? Notes { get; private set; }

    /// <summary>Lifecycle status. Pending does not touch stock.</summary>
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;

    /// <summary>Manual in slice 3; WhatsApp in a later slice.</summary>
    public OrderSource Source { get; private set; } = OrderSource.Manual;

    /// <summary>WhatsApp unmatched free-text flag. Always false for manual orders.</summary>
    public bool NeedsClarification { get; private set; }

    /// <summary>Sum of line totals in GHS.</summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>Optimistic concurrency token. Starts at 1; increment on every status change.</summary>
    public long Version { get; private set; } = 1;

    /// <summary>JWT user who created the order, when known.</summary>
    public string? CreatedByUserId { get; private set; }

    /// <summary>UTC insert time. Used for monthly plan-cap counts.</summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>UTC last status mutation.</summary>
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>UTC instant when the order became Confirmed, if ever.</summary>
    public DateTime? ConfirmedAt { get; private set; }

    /// <summary>UTC instant when the order became Paid. Dashboard “today” sales use this, not CreatedAt.</summary>
    public DateTime? PaidAt { get; private set; }

    /// <summary>UTC instant when the order became Fulfilled, if ever.</summary>
    public DateTime? FulfilledAt { get; private set; }

    /// <summary>UTC instant when the order became Cancelled, if ever.</summary>
    public DateTime? CancelledAt { get; private set; }

    /// <summary>Owning shop navigation. Required by EF; do not use in handlers.</summary>
    public Shop Shop { get; private set; } = null!;

    /// <summary>Line snapshots. Loaded when explicitly included or via <c>GetTracked</c>.</summary>
    public ICollection<OrderLine> Lines { get; private set; } = [];

    private Order()
    {
    }

    /// <summary>
    /// True when the order still represents a completed sale. Cancelled keeps <see cref="PaidAt"/> for audit
    /// but must not appear in dashboard <c>todaysSales</c> / paid-order count.
    /// </summary>
    public static bool CountsTowardTodaysSales(OrderStatus status) =>
        status is OrderStatus.Paid or OrderStatus.Fulfilled;

    /// <summary>
    /// True when <paramref name="from"/> may move to <paramref name="to"/>.
    /// Fulfilled and Cancelled are terminal; Pending cannot skip Confirmed to Paid.
    /// </summary>
    public static bool CanTransition(OrderStatus from, OrderStatus to) => (from, to) switch
    {
        (OrderStatus.Pending, OrderStatus.Confirmed) => true,
        (OrderStatus.Pending, OrderStatus.Cancelled) => true,
        (OrderStatus.Confirmed, OrderStatus.Paid) => true,
        (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
        (OrderStatus.Paid, OrderStatus.Fulfilled) => true,
        (OrderStatus.Paid, OrderStatus.Cancelled) => true,
        _ => false
    };

    /// <summary>
    /// Creates a manual Pending order with price snapshots. Does not reserve stock — call <see cref="TransitionTo"/> then the handler’s atomic UPDATE.
    /// </summary>
    public static Order CreateManual(
        string shopId,
        string customerName,
        string? customerPhone,
        string? notes,
        IReadOnlyList<OrderLineDraft> lines,
        string? createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shopId);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);

        if (lines is null || lines.Count == 0)
            throw new ArgumentException("An order must have at least one line.", nameof(lines));

        if (lines.Count > OrderConstraints.MaxLinesPerOrder)
            throw new ArgumentOutOfRangeException(nameof(lines), $"An order cannot exceed {OrderConstraints.MaxLinesPerOrder} lines.");

        var productIds = lines.Select(l => l.ProductId.Trim()).ToList();
        if (productIds.Distinct(StringComparer.Ordinal).Count() != productIds.Count)
            throw new ArgumentException("An order cannot contain the same product twice. Combine quantities on one line.", nameof(lines));

        var order = new Order
        {
            ShopId = shopId.Trim(),
            CustomerName = NormalizeRequired(customerName, OrderConstraints.CustomerNameMaxLength, nameof(customerName)),
            CustomerPhone = NormalizeOptional(customerPhone, OrderConstraints.CustomerPhoneMaxLength, nameof(customerPhone)),
            Notes = NormalizeOptional(notes, OrderConstraints.NotesMaxLength, nameof(notes)),
            Status = OrderStatus.Pending,
            Source = OrderSource.Manual,
            NeedsClarification = false,
            CreatedByUserId = string.IsNullOrWhiteSpace(createdByUserId) ? null : createdByUserId.Trim(),
            Version = 1
        };

        foreach (var draft in lines)
        {
            order.Lines.Add(OrderLine.Create(
                order.Id,
                order.ShopId,
                draft.ProductId,
                draft.ProductName,
                draft.Sku,
                draft.Quantity,
                draft.UnitPrice));
        }

        order.TotalAmount = decimal.Round(order.Lines.Sum(l => l.LineTotal), 2, MidpointRounding.AwayFromZero);
        return order;
    }

    /// <summary>
    /// Moves to <paramref name="to"/> and records the matching timestamp. Callers must apply stock in the same transaction.
    /// </summary>
    /// <exception cref="InvalidOperationException">Transition is not allowed from the current status.</exception>
    public void TransitionTo(OrderStatus to, DateTime utcNow)
    {
        if (!CanTransition(Status, to))
            throw new InvalidOperationException($"Cannot change a {Status} order to {to}.");

        Status = to;
        Version += 1;
        UpdatedAt = utcNow;

        switch (to)
        {
            case OrderStatus.Confirmed:
                ConfirmedAt = utcNow;
                break;
            case OrderStatus.Paid:
                PaidAt = utcNow;
                break;
            case OrderStatus.Fulfilled:
                FulfilledAt = utcNow;
                break;
            case OrderStatus.Cancelled:
                CancelledAt = utcNow;
                break;
        }
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

/// <summary>Catalog snapshot used when creating an <see cref="OrderLine"/>. Price comes from the product, not the client.</summary>
public sealed record OrderLineDraft(
    string ProductId,
    string ProductName,
    string Sku,
    int Quantity,
    decimal UnitPrice);
