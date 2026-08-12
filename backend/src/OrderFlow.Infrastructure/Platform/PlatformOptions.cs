namespace OrderFlow.Infrastructure.Platform;

public sealed class PlatformOptions
{
    public const string SectionName = "Platform";

    public string BaseUrl { get; set; } = "http://localhost:5176";

    public string IntegrationKey { get; set; } = string.Empty;

    public string ServiceCode { get; set; } = "ORDERFLOW";
}
