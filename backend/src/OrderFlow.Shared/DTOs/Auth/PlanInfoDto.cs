namespace OrderFlow.Shared.DTOs.Auth;

public class PlanInfoDto
{
    public required string Name { get; set; }

    public string? OriginalName { get; set; }

    public bool IsUnrecognized { get; set; }

    public int? MaxProducts { get; set; }

    public int? MaxOrdersPerMonth { get; set; }

    public int MaxUsers { get; set; }

    public bool AiFeatures { get; set; }

    public DateTime? ExpiresAt { get; set; }
}
