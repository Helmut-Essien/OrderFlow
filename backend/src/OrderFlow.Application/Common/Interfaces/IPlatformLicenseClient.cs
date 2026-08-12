namespace OrderFlow.Application.Common.Interfaces;

public sealed record LicenseValidationResult(
    bool IsValid,
    string? PlanName,
    DateTime? ExpiresAt,
    string? Message);

public interface IPlatformLicenseClient
{
    Task<LicenseValidationResult> ValidateAsync(string licenseKey, CancellationToken cancellationToken = default);
}
