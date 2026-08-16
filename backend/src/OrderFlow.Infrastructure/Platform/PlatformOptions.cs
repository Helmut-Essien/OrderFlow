namespace OrderFlow.Infrastructure.Platform;

/// <summary>Platform HTTP settings from config section <c>Platform</c> (env <c>PLATFORM__*</c>). Never log <see cref="IntegrationKey"/>.</summary>
public sealed class PlatformOptions
{
    /// <summary>Config section name. Bind with env <c>PLATFORM__*</c>.</summary>
    public const string SectionName = "Platform";

    /// <summary>Platform base URL. Production must not be localhost.</summary>
    public string BaseUrl { get; set; } = "http://localhost:5176";

    /// <summary>Sent as <c>X-Integration-Key</c>. Never log. Production must not use the Development key.</summary>
    public string IntegrationKey { get; set; } = string.Empty;

    /// <summary>Must stay <c>ORDERFLOW</c> so Platform validates the correct product.</summary>
    public string ServiceCode { get; set; } = "ORDERFLOW";
}
