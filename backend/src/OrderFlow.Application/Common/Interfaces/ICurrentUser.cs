namespace OrderFlow.Application.Common.Interfaces;

/// <summary>
/// Authenticated principal from the OrderFlow JWT. <see cref="ShopId"/> is the tenant for all business queries.
/// </summary>
public interface ICurrentUser
{
    string? UserId { get; }

    /// <summary>Tenant shop id from the <c>shopId</c> claim. Null when anonymous.</summary>
    string? ShopId { get; }

    string? Role { get; }

    bool IsAuthenticated { get; }
}
