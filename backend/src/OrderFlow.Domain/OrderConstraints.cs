namespace OrderFlow.Domain;

/// <summary>
/// Canonical order field limits. FluentValidation, EF, Shared DTOs, and Angular <c>ORDER_FIELD_LIMITS</c> must match these values.
/// </summary>
public static class OrderConstraints
{
    /// <summary>Customer display name max length.</summary>
    public const int CustomerNameMaxLength = 200;

    /// <summary>Optional customer phone max length (same bound as shop phone).</summary>
    public const int CustomerPhoneMaxLength = 50;

    /// <summary>Optional shop notes max length.</summary>
    public const int NotesMaxLength = 400;

    /// <summary>Maximum line items on one order (DoS / payload bound).</summary>
    public const int MaxLinesPerOrder = 50;

    /// <summary>Minimum units per line.</summary>
    public const int MinLineQuantity = 1;

    /// <summary>Maximum units per line (same upper bound as on-hand stock).</summary>
    public const int MaxLineQuantity = ProductConstraints.MaxStock;
}
