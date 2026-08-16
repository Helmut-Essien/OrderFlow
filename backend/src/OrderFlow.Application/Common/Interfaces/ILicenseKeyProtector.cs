namespace OrderFlow.Application.Common.Interfaces;

/// <summary>
/// ASP.NET Data Protection wrapper for Platform license keys. Persist only the protected payload; never log plaintext.
/// </summary>
public interface ILicenseKeyProtector
{
    /// <summary>Encrypts a Platform license key for storage. Never log <paramref name="licenseKey"/>.</summary>
    string Protect(string licenseKey);

    /// <summary>Decrypts a stored license payload. Used only when re-validating with Platform.</summary>
    string Unprotect(string protectedLicenseKey);
}
