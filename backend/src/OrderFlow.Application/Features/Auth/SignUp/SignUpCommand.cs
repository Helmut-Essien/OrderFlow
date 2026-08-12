using MediatR;
using OrderFlow.Shared.DTOs.Auth;

namespace OrderFlow.Application.Features.Auth.SignUp;

public sealed record SignUpCommand(
    string LicenseKey,
    string Email,
    string Password,
    string ShopName,
    string? DisplayName,
    string? Phone) : IRequest<AuthResponse>;
