namespace OrderFlow.Domain.Enums;

/// <summary>
/// Shop WhatsApp number connection state. Stored as a PostgreSQL string. MVP starts Disconnected until the WhatsApp slice.
/// </summary>
public enum WhatsAppConnectionStatus
{
    Disconnected = 0,
    Connected = 1,
    Error = 2
}
