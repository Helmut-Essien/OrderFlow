using System.Security.Cryptography;
using System.Text;

namespace OrderFlow.Domain;

/// <summary>
/// SHA-256 hex hasher for Platform license keys. Use the hash for uniqueness lookups; never persist or log the plaintext key.
/// </summary>
public static class LicenseLookupHasher
{
    /// <summary>Returns a 64-character lowercase hex SHA-256 digest of <paramref name="licenseKey"/>.</summary>
    public static string Compute(string licenseKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(licenseKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
