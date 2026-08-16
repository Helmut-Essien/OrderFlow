namespace OrderFlow.Domain.Enums;

/// <summary>
/// Shop WhatsApp number connection state. Stored as a PostgreSQL string. MVP starts Disconnected until the WhatsApp slice.
/// </summary>
public enum WhatsAppConnectionStatus
{
    /// <summary>No WhatsApp number linked yet (MVP default).</summary>
    Disconnected = 0,

    /// <summary>Shop number is linked and receiving messages.</summary>
    Connected = 1,

    /// <summary>Link failed; UI should prompt reconnect.</summary>
    Error = 2
}
