using Microsoft.AspNetCore.DataProtection;
using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Infrastructure.Identity;

/// <summary>Data Protection wrapper for Platform license keys. Purpose string is versioned; never log inputs or outputs.</summary>
public sealed class LicenseKeyProtector(IDataProtectionProvider provider) : ILicenseKeyProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("OrderFlow.LicenseKey.v1");

    /// <inheritdoc />
    public string Protect(string licenseKey) => _protector.Protect(licenseKey);

    /// <inheritdoc />
    public string Unprotect(string protectedLicenseKey) => _protector.Unprotect(protectedLicenseKey);
}
