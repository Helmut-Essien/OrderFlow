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

    public int? MaxProducts { get; set; }

    public int? MaxOrdersPerMonth { get; set; }

    public int MaxUsers { get; set; }

    public bool AiFeatures { get; set; }

    public DateTime? ExpiresAt { get; set; }
}
