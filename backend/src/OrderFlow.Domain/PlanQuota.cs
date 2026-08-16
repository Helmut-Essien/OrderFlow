namespace OrderFlow.Domain;

/// <summary>
/// OrderFlow-enforced plan limits mapped from Platform <c>planName</c>. Null max values mean unlimited.
/// </summary>
/// <remarks>
/// Matching is prefix-based and case-insensitive (Starter / Growth / Business).
/// Unknown names map to Starter limits with <see cref="IsUnrecognized"/> set so the dashboard can warn.
/// </remarks>
public sealed record PlanQuota(
    string Name,
    int? MaxProducts,
    int? MaxOrdersPerMonth,
    int MaxUsers,
    bool AiFeatures,
    bool IsUnrecognized = false,
    string? OriginalName = null)
{
    /// <summary>50 active SKUs, 300 orders/month, 1 user, no AI.</summary>
    public static PlanQuota Starter => new("Starter", 50, 300, 1, false);

    /// <summary>300 active SKUs, unlimited orders, 3 users, no AI.</summary>
    public static PlanQuota Growth => new("Growth", 300, null, 3, false);

    /// <summary>Unlimited SKUs and orders, 10 users, AI features enabled.</summary>
    public static PlanQuota Business => new("Business", null, null, 10, true);

    /// <summary>
    /// Maps a Platform plan name to quotas. Empty or unrecognized names return Starter with <see cref="IsUnrecognized"/> true.
    /// </summary>
    public static PlanQuota FromPlanName(string? planName)
    {
        if (string.IsNullOrWhiteSpace(planName))
            return Starter with { IsUnrecognized = true, OriginalName = planName };

        var normalized = planName.Trim();

        if (normalized.StartsWith("Business", StringComparison.OrdinalIgnoreCase))
            return Business with { OriginalName = planName };

        if (normalized.StartsWith("Growth", StringComparison.OrdinalIgnoreCase))
            return Growth with { OriginalName = planName };

        if (normalized.StartsWith("Starter", StringComparison.OrdinalIgnoreCase))
            return Starter with { OriginalName = planName };

        return Starter with { IsUnrecognized = true, OriginalName = planName };
    }
}
