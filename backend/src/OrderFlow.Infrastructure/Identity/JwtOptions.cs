namespace OrderFlow.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "OrderFlow.Api";

    public string Audience { get; set; } = "OrderFlow.Frontend";

    public string Key { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 480;
}
