namespace OrderFlow.Shared.Constants;

/// <summary>Custom JWT claim types. <see cref="ShopId"/> is the tenant for EF global query filters.</summary>
public static class ClaimNames
{
    public const string ShopId = "shopId";
    public const string PlanName = "planName";
}
