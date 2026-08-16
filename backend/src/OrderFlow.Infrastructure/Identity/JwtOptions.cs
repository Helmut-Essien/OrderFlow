namespace OrderFlow.Infrastructure.Identity;

/// <summary>JWT settings from config section <c>Jwt</c> (env <c>JWT__*</c>). Production key must be ≥ 64 characters.</summary>
public sealed class JwtOptions
{
    /// <summary>Config section name. Bind with env <c>JWT__*</c>.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Token issuer. Must match validation in <c>JwtBearerConfigurator</c>.</summary>
    public string Issuer { get; set; } = "OrderFlow.Api";

    /// <summary>Token audience. Must match the Angular API consumer.</summary>
    public string Audience { get; set; } = "OrderFlow.Frontend";

    /// <summary>HMAC signing key. Never log. Production must be ≥ 64 unique characters.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Access-token lifetime in minutes (default 8 hours).</summary>
    public int ExpiryMinutes { get; set; } = 480;
}
