using OrderFlow.Domain.Enums;

namespace OrderFlow.Domain.Entities;

/// <summary>
/// Shop staff account. Email is unique globally, stored lowercase, and is the login identifier (not the license key).
/// </summary>
public class User
{
    public string Id { get; private set; } = NUlid.Ulid.NewUlid().ToString();

    /// <summary>Tenant shop this user belongs to.</summary>
    public string ShopId { get; private set; } = string.Empty;

    /// <summary>Lowercase email, unique, max 320 characters.</summary>
    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>BCrypt hash. Never log or return this value.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; } = UserRole.Assistant;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public Shop Shop { get; private set; } = null!;

    private User()
    {
    }

    /// <summary>Creates the first user for a shop (signup). Role is always <see cref="UserRole.Owner"/>.</summary>
    public static User CreateOwner(string shopId, string email, string displayName, string passwordHash)
        => Create(shopId, email, displayName, passwordHash, UserRole.Owner);

    /// <summary>Creates an assistant user (settings slice). Role is always <see cref="UserRole.Assistant"/>.</summary>
    public static User CreateAssistant(string shopId, string email, string displayName, string passwordHash)
        => Create(shopId, email, displayName, passwordHash, UserRole.Assistant);

    private static User Create(
        string shopId,
        string email,
        string displayName,
        string passwordHash,
        UserRole role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shopId);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (normalizedEmail.Length > 320)
            throw new ArgumentOutOfRangeException(nameof(email), "Email cannot exceed 320 characters.");

        var normalizedDisplayName = displayName.Trim();
        if (normalizedDisplayName.Length > 200)
            throw new ArgumentOutOfRangeException(nameof(displayName), "Display name cannot exceed 200 characters.");

        return new User
        {
            ShopId = shopId,
            Email = normalizedEmail,
            DisplayName = normalizedDisplayName,
            PasswordHash = passwordHash,
            Role = role
        };
    }
}
