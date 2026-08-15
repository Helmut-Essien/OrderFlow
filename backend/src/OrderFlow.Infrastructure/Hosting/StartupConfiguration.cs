using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OrderFlow.Infrastructure.Identity;
using OrderFlow.Infrastructure.Platform;

namespace OrderFlow.Infrastructure.Hosting;

/// <summary>
/// Fail-fast checks so Production cannot boot with Development JWT keys, Platform credentials, or a leaky connection string.
/// </summary>
public static class StartupConfiguration
{
    /// <summary>Committed Development JWT signing key. Production must override via <c>JWT__KEY</c>.</summary>
    public const string DevelopmentJwtKey = "OrderFlow_Dev_Jwt_Signing_Key_Change_In_Production_Min32";

    /// <summary>Committed Development Platform integration key. Production must override via <c>PLATFORM__INTEGRATIONKEY</c>.</summary>
    public const string DevelopmentIntegrationKey = "ORDERFLOW-INTEGRATION-DEV-KEY-1b7e3c4a5d8f";

    /// <summary>Minimum HMAC key length in every environment (HS256 practical floor).</summary>
    public const int MinimumJwtKeyLength = 32;

    /// <summary>Production HMAC key length required by the OrderFlow skill (≥ 64 characters).</summary>
    public const int ProductionJwtKeyLength = 64;

    /// <summary>
    /// Validates connection string, JWT, and Platform settings. Development may use committed defaults; Production may not.
    /// </summary>
    /// <exception cref="InvalidOperationException">A required setting is missing or is a known Development secret in Production.</exception>
    public static void Validate(IConfiguration configuration, string environmentName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        var isProduction = string.Equals(environmentName, Environments.Production, StringComparison.OrdinalIgnoreCase);
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var platform = configuration.GetSection(PlatformOptions.SectionName).Get<PlatformOptions>() ?? new PlatformOptions();

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < MinimumJwtKeyLength)
            throw new InvalidOperationException($"Jwt:Key must be at least {MinimumJwtKeyLength} characters.");

        if (string.IsNullOrWhiteSpace(platform.BaseUrl))
            throw new InvalidOperationException("Platform:BaseUrl is required.");

        if (!isProduction)
            return;

        if (jwt.Key.Length < ProductionJwtKeyLength)
            throw new InvalidOperationException($"Jwt:Key must be at least {ProductionJwtKeyLength} characters in Production.");

        if (string.Equals(jwt.Key, DevelopmentJwtKey, StringComparison.Ordinal))
            throw new InvalidOperationException("Jwt:Key is the Development signing key. Set JWT__KEY to a unique Production secret.");

        if (connectionString.Contains("Password=orderflow_dev", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection still uses the Development Postgres password.");

        if (connectionString.Contains("Include Error Detail", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection must not enable Include Error Detail in Production.");

        if (platform.BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || platform.BaseUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Platform:BaseUrl must not point at localhost in Production.");

        if (string.IsNullOrWhiteSpace(platform.IntegrationKey))
            throw new InvalidOperationException("Platform:IntegrationKey is required in Production (PLATFORM__INTEGRATIONKEY).");

        if (string.Equals(platform.IntegrationKey, DevelopmentIntegrationKey, StringComparison.Ordinal))
            throw new InvalidOperationException("Platform:IntegrationKey is the Development key. Set PLATFORM__INTEGRATIONKEY from Platform.");
    }
}
