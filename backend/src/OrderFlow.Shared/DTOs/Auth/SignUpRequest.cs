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

    /// <summary>Login identifier; stored lowercase, max 320.</summary>
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Plaintext password (min 8, max 128). Never logged.</summary>
    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Shop display name, max 200.</summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string ShopName { get; set; } = string.Empty;

    /// <summary>Owner display name; defaults to the email local-part when omitted.</summary>
    [StringLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>Optional shop phone, max 50.</summary>
    [StringLength(50)]
    public string? Phone { get; set; }
}
