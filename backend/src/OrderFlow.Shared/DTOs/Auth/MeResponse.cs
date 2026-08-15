namespace OrderFlow.Shared.DTOs.Auth;

/// <summary>Current session without a new token. Used by <c>GET /api/auth/me</c> to hydrate shop/plan Signals.</summary>
public class MeResponse
{
    public required string ShopId { get; set; }

    public required string ShopName { get; set; }

    public required string UserId { get; set; }

    public required string Email { get; set; }

    public required string DisplayName { get; set; }

    public required string Role { get; set; }

    public required PlanInfoDto Plan { get; set; }
}
