namespace OrderFlow.Domain.Enums;

/// <summary>
/// How the order entered the shop. Stored as a PostgreSQL string with a CHECK constraint.
/// Slice 3 creates <see cref="Manual"/> only; WhatsApp webhooks come in a later slice.
/// </summary>
public enum OrderSource
{
    /// <summary>Shop staff entered the order in the dashboard.</summary>
    Manual = 0,

    /// <summary>Customer message on the shop WhatsApp number (later slice).</summary>
    WhatsApp = 1
}
