using OrderFlow.Domain.Enums;

namespace OrderFlow.Domain.Entities;

public class User
{
    public string Id { get; private set; } = NUlid.Ulid.NewUlid().ToString();

    public string ShopId { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; } = UserRole.Assistant;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public Shop Shop { get; private set; } = null!;

    private User()
    {
    }

    public static User CreateOwner(string shopId, string email, string displayName, string passwordHash)
    {
        return new User
        {
            ShopId = shopId,
            Email = email,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            Role = UserRole.Owner
        };
    }

    public static User CreateAssistant(string shopId, string email, string displayName, string passwordHash)
    {
        return new User
        {
            ShopId = shopId,
            Email = email,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            Role = UserRole.Assistant
        };
    }
}
