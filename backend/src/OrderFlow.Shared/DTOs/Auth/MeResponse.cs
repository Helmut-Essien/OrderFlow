namespace OrderFlow.Shared.DTOs.Auth;

/// <summary>Current session without a new token. Used by <c>GET /api/auth/me</c> to hydrate shop/plan Signals.</summary>
public class MeResponse
{
    /// <summary>Tenant shop id (also in the JWT <c>shopId</c> claim).</summary>
    public required string ShopId { get; set; }

    /// <summary>Shop display name for the shell header.</summary>
    public required string ShopName { get; set; }

    /// <summary>Authenticated user id (JWT <c>sub</c>).</summary>
    public required string UserId { get; set; }

    /// <summary>Lowercase login email.</summary>
    public required string Email { get; set; }

    /// <summary>Shown in the shell; not the login identifier.</summary>
    public required string DisplayName { get; set; }

    /// <summary><c>Owner</c> or <c>Assistant</c>.</summary>
    public required string Role { get; set; }

    /// <summary>Mapped Platform plan snapshot, including unrecognized-plan warning.</summary>
    public required PlanInfoDto Plan { get; set; }
}
