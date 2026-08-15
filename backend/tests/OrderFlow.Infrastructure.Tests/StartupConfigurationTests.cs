using Microsoft.Extensions.Configuration;
using OrderFlow.Infrastructure.Hosting;

namespace OrderFlow.Infrastructure.Tests;

public class StartupConfigurationTests
{
    [Fact]
    public void Validate_AllowsDevelopmentDefaults()
    {
        var config = Build(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Host=localhost;Port=5433;Database=orderflow_db;Username=orderflow;Password=orderflow_dev;Include Error Detail=true",
            ["Jwt:Key"] = StartupConfiguration.DevelopmentJwtKey,
            ["Platform:BaseUrl"] = "http://localhost:5176",
            ["Platform:IntegrationKey"] = StartupConfiguration.DevelopmentIntegrationKey
        });

        StartupConfiguration.Validate(config, "Development");
    }

    [Fact]
    public void Validate_AllowsProductionWhenSecretsAreReplaced()
    {
        var config = ProductionConfig();

        StartupConfiguration.Validate(config, "Production");
    }

    [Fact]
    public void Validate_Throws_WhenProductionUsesDevelopmentJwtKey()
    {
        var config = ProductionConfig(jwtKey: StartupConfiguration.DevelopmentJwtKey);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            StartupConfiguration.Validate(config, "Production"));

        Assert.Contains("64", ex.Message);
    }

    [Fact]
    public void Validate_Throws_WhenProductionJwtKeyIsShorterThan64()
    {
        var config = ProductionConfig(jwtKey: new string('k', 32));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            StartupConfiguration.Validate(config, "Production"));

        Assert.Contains("64", ex.Message);
    }

    [Fact]
    public void Validate_Throws_WhenProductionUsesDevelopmentIntegrationKey()
    {
        var config = ProductionConfig(integrationKey: StartupConfiguration.DevelopmentIntegrationKey);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            StartupConfiguration.Validate(config, "Production"));

        Assert.Contains("Development key", ex.Message);
    }

    [Fact]
    public void Validate_Throws_WhenProductionPlatformIsLocalhost()
    {
        var config = ProductionConfig(platformBaseUrl: "https://localhost:5176");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            StartupConfiguration.Validate(config, "Production"));

        Assert.Contains("localhost", ex.Message);
    }

    [Fact]
    public void Validate_Throws_WhenProductionConnectionStringLeaksSqlDetails()
    {
        var config = ProductionConfig(connectionString:
            "Host=db;Database=orderflow_db;Username=orderflow;Password=prod-secret;Include Error Detail=true");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            StartupConfiguration.Validate(config, "Production"));

        Assert.Contains("Include Error Detail", ex.Message);
    }

    [Fact]
    public void Validate_Throws_WhenJwtKeyIsMissing()
    {
        var config = Build(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=orderflow_db;Username=orderflow;Password=x",
            ["Platform:BaseUrl"] = "http://localhost:5176"
        });

        Assert.Throws<InvalidOperationException>(() =>
            StartupConfiguration.Validate(config, "Development"));
    }

    private static IConfiguration ProductionConfig(
        string? jwtKey = null,
        string? integrationKey = null,
        string? platformBaseUrl = null,
        string? connectionString = null)
    {
        return Build(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connectionString
                ?? "Host=db.internal;Database=orderflow_db;Username=orderflow;Password=prod-secret",
            ["Jwt:Key"] = jwtKey ?? new string('p', StartupConfiguration.ProductionJwtKeyLength),
            ["Platform:BaseUrl"] = platformBaseUrl ?? "https://platform.example",
            ["Platform:IntegrationKey"] = integrationKey ?? "prod-integration-key"
        });
    }

    private static IConfiguration Build(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
