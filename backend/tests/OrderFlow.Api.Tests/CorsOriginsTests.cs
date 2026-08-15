using Microsoft.Extensions.Configuration;
using OrderFlow.Api.Hosting;

namespace OrderFlow.Api.Tests;

public class CorsOriginsTests
{
    [Fact]
    public void Resolve_ReadsJsonArray()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:Origins:0"] = "https://app.example",
                ["Cors:Origins:1"] = " https://admin.example "
            })
            .Build();

        var origins = CorsOrigins.Resolve(config);

        Assert.Equal(["https://app.example", "https://admin.example"], origins);
    }

    [Fact]
    public void Resolve_ReadsCommaSeparatedEnvStyleValue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:Origins"] = "https://app.example, https://admin.example"
            })
            .Build();

        var origins = CorsOrigins.Resolve(config);

        Assert.Equal(["https://app.example", "https://admin.example"], origins);
    }

    [Fact]
    public void Resolve_ReturnsEmpty_WhenUnset()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

        Assert.Empty(CorsOrigins.Resolve(config));
    }
}
