using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Shared.DTOs.Auth;

public class SignUpRequest
{
    [Required]
    public string LicenseKey { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string ShopName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? Phone { get; set; }
}
