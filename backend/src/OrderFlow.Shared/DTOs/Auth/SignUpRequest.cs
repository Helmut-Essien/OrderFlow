using System.ComponentModel.DataAnnotations;

namespace OrderFlow.Shared.DTOs.Auth;

public class SignUpRequest
{
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
