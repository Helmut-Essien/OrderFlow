namespace OrderFlow.Application.Common.Interfaces;

/// <summary>
/// Authenticated principal from the OrderFlow JWT. <see cref="ShopId"/> is the tenant for all business queries.
/// </summary>
public interface ICurrentUser
{
    /// <summary>JWT <c>sub</c>. Null when anonymous.</summary>
    string? UserId { get; }

    /// <summary>Tenant shop id from the <c>shopId</c> claim. Null when anonymous.</summary>
    string? ShopId { get; }

    /// <summary>JWT role claim (<c>Owner</c> or <c>Assistant</c>). Null when anonymous.</summary>
    string? Role { get; }

    /// <summary>True when the request has a valid OrderFlow JWT.</summary>
    bool IsAuthenticated { get; }
}
