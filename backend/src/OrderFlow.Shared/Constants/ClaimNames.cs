namespace OrderFlow.Shared.Constants;

/// <summary>Custom JWT claim types. <see cref="ShopId"/> is the tenant for EF global query filters.</summary>
public static class ClaimNames
{
    /// <summary>JWT claim holding the tenant shop id for EF global query filters.</summary>
    public const string ShopId = "shopId";

    /// <summary>JWT claim holding the Platform plan name snapshot.</summary>
    public const string PlanName = "planName";
}
