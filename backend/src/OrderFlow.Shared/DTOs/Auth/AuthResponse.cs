namespace OrderFlow.Shared.DTOs.Auth;

public class AuthResponse
{
    public required string Token { get; set; }

    public required DateTime ExpiresAt { get; set; }

    public required string ShopId { get; set; }

    public required string ShopName { get; set; }

    public required string UserId { get; set; }

    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    public required string Role { get; set; }

    public required PlanInfoDto Plan { get; set; }
}
