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
        return new Shop
        {
            Name = name,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            LicenseLookupHash = licenseLookupHash,
            ProtectedLicenseKey = protectedLicenseKey,
            PlanName = planName,
            PlanExpiresAt = planExpiresAt,
            PlanUnrecognized = planUnrecognized
        };
    }

    public void UpdatePlanSnapshot(string? planName, DateTime? expiresAt, bool unrecognized)
    {
        PlanName = string.IsNullOrWhiteSpace(planName) ? PlanName : planName;
        PlanExpiresAt = expiresAt;
        PlanUnrecognized = unrecognized;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string name, string? phone, string? address)
    {
        Name = name;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
