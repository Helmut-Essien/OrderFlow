using System.Security.Cryptography;
using System.Text;

namespace OrderFlow.Domain;

public static class LicenseLookupHasher
{
    public static string Compute(string licenseKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(licenseKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
