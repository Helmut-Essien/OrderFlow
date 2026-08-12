namespace OrderFlow.Application.Common.Interfaces;

public interface ILicenseKeyProtector
{
    string Protect(string licenseKey);

    string Unprotect(string protectedLicenseKey);
}
