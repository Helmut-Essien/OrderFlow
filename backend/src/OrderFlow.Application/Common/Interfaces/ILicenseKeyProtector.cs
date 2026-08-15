namespace OrderFlow.Application.Common.Interfaces;

/// <summary>
/// ASP.NET Data Protection wrapper for Platform license keys. Persist only the protected payload; never log plaintext.
/// </summary>
public interface ILicenseKeyProtector
{
    string Protect(string licenseKey);

    string Unprotect(string protectedLicenseKey);
}
