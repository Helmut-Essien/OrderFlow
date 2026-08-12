using Microsoft.AspNetCore.DataProtection;
using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Infrastructure.Identity;

public sealed class LicenseKeyProtector(IDataProtectionProvider provider) : ILicenseKeyProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("OrderFlow.LicenseKey.v1");

    public string Protect(string licenseKey) => _protector.Protect(licenseKey);

    public string Unprotect(string protectedLicenseKey) => _protector.Unprotect(protectedLicenseKey);
}
