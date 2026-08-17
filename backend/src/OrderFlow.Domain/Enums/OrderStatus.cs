namespace OrderFlow.Domain.Enums;

/// <summary>
/// Shop-facing order lifecycle. Stored as a PostgreSQL string with a CHECK constraint.
/// Pending WhatsApp drafts do not touch stock; reserve on Confirmed, deduct on Paid, release on Cancelled.
/// </summary>
public enum OrderStatus
{
    /// <summary>Draft. WhatsApp unmatched text starts here; stock is unchanged.</summary>
    Pending = 0,

    /// <summary>Accepted. On-hand stock is reserved (atomic decrement).</summary>
    Confirmed = 1,

    /// <summary>Payment recorded. Reserved qty is already off the shelf; deduct is an audit movement only.</summary>
    Paid = 2,

    /// <summary>Handed to the customer. Terminal happy path; stock is unchanged.</summary>
    Fulfilled = 3,

    /// <summary>Abandoned. Releases reserved qty when the order had already been Confirmed or Paid.</summary>
    Cancelled = 4
}
