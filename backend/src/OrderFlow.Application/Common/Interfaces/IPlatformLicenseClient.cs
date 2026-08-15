namespace OrderFlow.Application.Common.Interfaces;

/// <summary>
/// Result of <c>POST /api/licenses/validate</c> on Platform. Invalid licenses still return HTTP 200 with <see cref="IsValid"/> false.
/// </summary>
public sealed record LicenseValidationResult(
    bool IsValid,
    string? PlanName,
    DateTime? ExpiresAt,
    string? Message);

/// <summary>
/// HTTP client for Platform license validation only. Must send <c>X-Integration-Key</c>; must not call Platform admin APIs.
/// </summary>
public interface IPlatformLicenseClient
{
    /// <summary>Validates a plaintext license key. Callers must not log <paramref name="licenseKey"/>.</summary>
    Task<LicenseValidationResult> ValidateAsync(string licenseKey, CancellationToken cancellationToken = default);
}
