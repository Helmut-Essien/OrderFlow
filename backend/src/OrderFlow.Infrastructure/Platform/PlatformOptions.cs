namespace OrderFlow.Infrastructure.Platform;

/// <summary>Platform HTTP settings from config section <c>Platform</c> (env <c>PLATFORM__*</c>). Never log <see cref="IntegrationKey"/>.</summary>
public sealed class PlatformOptions
{
    public const string SectionName = "Platform";

    public string BaseUrl { get; set; } = "http://localhost:5176";

    public string IntegrationKey { get; set; } = string.Empty;

    public string ServiceCode { get; set; } = "ORDERFLOW";
}
