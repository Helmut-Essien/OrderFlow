using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Shared.DTOs.Auth;

/// <summary>Login body. Email/password only — no license key. Password max 128 is a payload bound, not a min-length rule.</summary>
public class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}
