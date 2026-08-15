using System.Net;

namespace OrderFlow.Api.Tests;

[Collection("OrderFlowApi")]
public class HealthEndpointsTests
{
    private readonly OrderFlowApiFactory _factory;

    public HealthEndpointsTests(OrderFlowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsOk_WithoutAuthentication()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
