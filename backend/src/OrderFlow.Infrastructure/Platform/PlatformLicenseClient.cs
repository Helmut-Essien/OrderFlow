using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Infrastructure.Platform;

/// <summary>
/// Calls Platform <c>POST /api/licenses/validate</c> with <c>X-Integration-Key</c>. Does not log the license key.
/// Network/parse failures return <c>IsValid = false</c> rather than throwing.
/// </summary>
public sealed class PlatformLicenseClient(
    HttpClient http,
    IOptions<PlatformOptions> options,
    ILogger<PlatformLicenseClient> logger) : IPlatformLicenseClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public async Task<LicenseValidationResult> ValidateAsync(
        string licenseKey,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/licenses/validate");
        // Integration key authenticates OrderFlow to Platform; never log this header or the license body.
        request.Headers.TryAddWithoutValidation("X-Integration-Key", settings.IntegrationKey);
        request.Content = JsonContent.Create(new
        {
            licenseKey,
            serviceCode = settings.ServiceCode
        });

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to reach Platform license validation API");
            return new LicenseValidationResult(false, null, null, "Unable to validate license. Please try again.");
        }

        PlatformValidateResponse? body;
        try
        {
            body = await response.Content.ReadFromJsonAsync<PlatformValidateResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Platform returned a non-JSON license validation response ({StatusCode})", (int)response.StatusCode);
            return new LicenseValidationResult(false, null, null, "Unable to validate license. Please try again.");
        }

        if (body is null)
            return new LicenseValidationResult(false, null, null, "Unable to validate license. Please try again.");

        return new LicenseValidationResult(body.IsValid, body.PlanName, body.ExpiresAt, body.Message);
    }

    private sealed class PlatformValidateResponse
    {
        public bool IsValid { get; set; }

        public string? PlanName { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public string? Message { get; set; }
    }
}
