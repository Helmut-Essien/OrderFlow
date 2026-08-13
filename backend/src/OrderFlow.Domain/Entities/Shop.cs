using OrderFlow.Domain.Enums;

namespace OrderFlow.Domain.Entities;

public class Shop
{
    public string Id { get; private set; } = NUlid.Ulid.NewUlid().ToString();

    public string Name { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public string? Address { get; private set; }

    public string LicenseLookupHash { get; private set; } = string.Empty;

    public string ProtectedLicenseKey { get; private set; } = string.Empty;

    public string PlanName { get; private set; } = "Starter";

    public DateTime? PlanExpiresAt { get; private set; }

    public bool PlanUnrecognized { get; private set; }

    public WhatsAppConnectionStatus WhatsAppConnectionStatus { get; private set; } =
        WhatsAppConnectionStatus.Disconnected;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; private set; } = [];

    private Shop()
    {
    }

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

    public void UpdatePlanSnapshot(string? planName, DateTime? expiresAt, bool unrecognized)
    {
        if (!string.IsNullOrWhiteSpace(planName))
            PlanName = planName.Trim();

        PlanExpiresAt = expiresAt;
        PlanUnrecognized = unrecognized;
        UpdatedAt = DateTime.UtcNow;
    }

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
