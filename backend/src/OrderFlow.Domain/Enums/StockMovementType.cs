namespace OrderFlow.Domain.Enums;

/// <summary>
/// Kind of inventory change. Stored as a PostgreSQL string with a CHECK constraint.
/// </summary>
public enum StockMovementType
{
    /// <summary>Manual stock correction (slice 2).</summary>
    Adjustment = 0,

    /// <summary>Hold stock when an order is Confirmed (slice 3+).</summary>
    Reserve = 1,

    /// <summary>Remove reserved stock when an order is Paid (slice 3+).</summary>
    Deduct = 2,

    /// <summary>Return reserved stock when an order is Cancelled (slice 3+).</summary>
    Release = 3
}
