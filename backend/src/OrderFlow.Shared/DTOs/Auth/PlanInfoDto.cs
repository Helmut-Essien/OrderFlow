namespace OrderFlow.Shared.DTOs.Auth;

/// <summary>
/// Plan snapshot mapped from Platform <c>planName</c>. Null max values mean unlimited.
/// </summary>
public class PlanInfoDto
{
    /// <summary>Canonical quota name (Starter / Growth / Business).</summary>
    public required string Name { get; set; }

    /// <summary>Raw Platform plan string when it differs from <see cref="Name"/>.</summary>
    public string? OriginalName { get; set; }

    /// <summary>True when the Platform name did not match a known plan; UI should show an amber warning.</summary>
    public bool IsUnrecognized { get; set; }

    /// <summary>Active SKU cap; null means unlimited.</summary>
    public int? MaxProducts { get; set; }

    /// <summary>Paid-order cap per calendar month; null means unlimited.</summary>
    public int? MaxOrdersPerMonth { get; set; }

    /// <summary>Staff-account cap including the owner.</summary>
    public int MaxUsers { get; set; }

    /// <summary>True on Business; AI parsing is a later slice.</summary>
    public bool AiFeatures { get; set; }

    /// <summary>UTC plan expiry from Platform; null if omitted.</summary>
    public DateTime? ExpiresAt { get; set; }
}
