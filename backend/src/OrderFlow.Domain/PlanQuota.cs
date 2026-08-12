namespace OrderFlow.Domain;

public sealed record PlanQuota(
    string Name,
    int? MaxProducts,
    int? MaxOrdersPerMonth,
    int MaxUsers,
    bool AiFeatures,
    bool IsUnrecognized = false,
    string? OriginalName = null)
{
    public static PlanQuota Starter => new("Starter", 50, 300, 1, false);

    public static PlanQuota Growth => new("Growth", 300, null, 3, false);

    public static PlanQuota Business => new("Business", null, null, 10, true);

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
