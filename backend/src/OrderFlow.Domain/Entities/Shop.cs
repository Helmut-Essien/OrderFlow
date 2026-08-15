using OrderFlow.Domain.Enums;

namespace OrderFlow.Domain.Entities;

/// <summary>
/// Tenant root. License keys are stored as a SHA-256 lookup hash plus a Data Protection payload — never plaintext.
/// </summary>
public class Shop
{
    public string Id { get; private set; } = NUlid.Ulid.NewUlid().ToString();

    public string Name { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public string? Address { get; private set; }

    /// <summary>64-character lowercase SHA-256 hex of the Platform license key, used for uniqueness lookups.</summary>
    public string LicenseLookupHash { get; private set; } = string.Empty;

    /// <summary>Data Protection ciphertext of the license key. Never log this value.</summary>
    public string ProtectedLicenseKey { get; private set; } = string.Empty;

    /// <summary>Platform plan name snapshot (Starter / Growth / Business). Unknown names still store the original string.</summary>
    public string PlanName { get; private set; } = "Starter";

    public DateTime? PlanExpiresAt { get; private set; }

    /// <summary>True when Platform returned a plan name that does not map to a known quota; UI should warn.</summary>
    public bool PlanUnrecognized { get; private set; }

    public WhatsAppConnectionStatus WhatsAppConnectionStatus { get; private set; } =
        WhatsAppConnectionStatus.Disconnected;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; private set; } = [];

    private Shop()
    {
    }

    /// <summary>
    /// Creates a shop from a validated Platform license. <paramref name="licenseLookupHash"/> must be exactly 64 hex chars.
    /// </summary>
    public static Shop Create(
        string name,
        string? phone,
        string licenseLookupHash,
        string protectedLicenseKey,
        string planName,
        DateTime? planExpiresAt,
        bool planUnrecognized)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(licenseLookupHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedLicenseKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(planName);

        if (licenseLookupHash.Length != 64)
            throw new ArgumentException("License lookup hash must be a 64-character SHA-256 hex string.", nameof(licenseLookupHash));

        if (name.Trim().Length > 200)
            throw new ArgumentOutOfRangeException(nameof(name), "Shop name cannot exceed 200 characters.");

        var normalizedPhone = NormalizeOptional(phone, 50, nameof(phone));

        return new Shop
        {
            Name = name.Trim(),
            Phone = normalizedPhone,
            LicenseLookupHash = licenseLookupHash,
            ProtectedLicenseKey = protectedLicenseKey,
            PlanName = planName.Trim(),
            PlanExpiresAt = planExpiresAt,
            PlanUnrecognized = planUnrecognized
        };
    }

    /// <summary>Refreshes the cached Platform plan after a later license validate call.</summary>
    public void UpdatePlanSnapshot(string? planName, DateTime? expiresAt, bool unrecognized)
    {
        if (!string.IsNullOrWhiteSpace(planName))
            PlanName = planName.Trim();

        PlanExpiresAt = expiresAt;
        PlanUnrecognized = unrecognized;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Updates display profile fields. Does not change license or plan.</summary>
    public void UpdateProfile(string name, string? phone, string? address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (name.Trim().Length > 200)
            throw new ArgumentOutOfRangeException(nameof(name), "Shop name cannot exceed 200 characters.");

        Name = name.Trim();
        Phone = NormalizeOptional(phone, 50, nameof(phone));
        Address = NormalizeOptional(address, 400, nameof(address));
        UpdatedAt = DateTime.UtcNow;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ArgumentOutOfRangeException(paramName, $"Value cannot exceed {maxLength} characters.");

        return trimmed;
    }
}
