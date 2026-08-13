using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Shared.DTOs.Auth;

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
