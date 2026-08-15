using MediatR;
using OrderFlow.Shared.DTOs.Auth;

namespace OrderFlow.Application.Features.Auth.SignUp;

/// <summary>
/// First-time shop registration. License key is required here only; later logins use email/password.
/// </summary>
public sealed record SignUpCommand(
    string LicenseKey,
    string Email,
    string Password,
    string ShopName,
    string? DisplayName,
    string? Phone) : IRequest<AuthResponse>;
