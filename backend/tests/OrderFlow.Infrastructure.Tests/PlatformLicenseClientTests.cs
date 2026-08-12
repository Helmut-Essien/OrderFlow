using System.Net;
using System.Text;
using OrderFlow.Infrastructure.Platform;

namespace OrderFlow.Infrastructure.Tests;

public class PlatformLicenseClientTests
{
    [Fact]
    public async Task ValidateAsync_MapsValidPlatformResponse()
    {
        var handler = new StubHandler("""
            {"isValid":true,"planName":"Growth","expiresAt":"2027-12-31T00:00:00Z","message":null}
            """, HttpStatusCode.OK);
        var client = CreateClient(handler);

        var result = await client.ValidateAsync("ORDERFLOW-DEVK-TEST");

        Assert.True(result.IsValid);
        Assert.Equal("Growth", result.PlanName);
        Assert.Equal("ORDERFLOW-DEVK-TEST", handler.LastLicenseKey);
        Assert.Equal("ORDERFLOW", handler.LastServiceCode);
        Assert.Equal("test-integration-key", handler.LastIntegrationKey);
    }

    [Fact]
    public async Task ValidateAsync_MapsInvalidLicense()
    {
        var handler = new StubHandler("""
            {"isValid":false,"planName":null,"expiresAt":null,"message":"Invalid license key."}
            """, HttpStatusCode.OK);
        var client = CreateClient(handler);

        var result = await client.ValidateAsync("bad-key");

        Assert.False(result.IsValid);
        Assert.Equal("Invalid license key.", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsFriendlyError_WhenPlatformIsUnreachable()
    {
        var handler = new StubHandler(throwOnSend: true);
        var client = CreateClient(handler);

        var result = await client.ValidateAsync("ORDERFLOW-DEVK-TEST");

        Assert.False(result.IsValid);
        Assert.Contains("Unable to validate license", result.Message);
    }

    private static PlatformLicenseClient CreateClient(StubHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://platform.test/") };
        var options = Microsoft.Extensions.Options.Options.Create(new PlatformOptions
        {
            BaseUrl = "http://platform.test",
            IntegrationKey = "test-integration-key",
            ServiceCode = "ORDERFLOW"
        });
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlatformLicenseClient>();
        return new PlatformLicenseClient(http, options, logger);
    }

    private sealed class StubHandler(string? json = null, HttpStatusCode status = HttpStatusCode.OK, bool throwOnSend = false)
        : HttpMessageHandler
    {
        public string? LastLicenseKey { get; private set; }

        public string? LastServiceCode { get; private set; }

        public string? LastIntegrationKey { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (throwOnSend)
                throw new HttpRequestException("connection refused");

            LastIntegrationKey = request.Headers.TryGetValues("X-Integration-Key", out var values)
                ? values.FirstOrDefault()
                : null;

            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            LastLicenseKey = ReadJsonProperty(body, "licenseKey");
            LastServiceCode = ReadJsonProperty(body, "serviceCode");

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(json ?? "{}", Encoding.UTF8, "application/json")
            };
        }

        private static string? ReadJsonProperty(string json, string name)
        {
            var token = $"\"{name}\":\"";
            var start = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                return null;
            start += token.Length;
            var end = json.IndexOf('"', start);
            return end < 0 ? null : json[start..end];
        }
    }
}
