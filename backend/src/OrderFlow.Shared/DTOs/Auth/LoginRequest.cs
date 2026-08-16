using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Shared.DTOs.Auth;

/// <summary>Login body. Email/password only — no license key. Password max 128 is a payload bound, not a min-length rule.</summary>
public class LoginRequest
{
    /// <summary>Login identifier; compared lowercase, max 320.</summary>
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Plaintext password. Max 128 is a payload bound; min length is not enforced at login.</summary>
    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}
