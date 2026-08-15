using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Shared.DTOs.Auth;

/// <summary>
/// Signup body. License key is required only here; Angular <c>AUTH_FIELD_LIMITS</c> must match these lengths.
/// </summary>
public class SignUpRequest
{
    /// <summary>Platform license key. Never logged. Max 100.</summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string LicenseKey { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string ShopName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? DisplayName { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }
}
