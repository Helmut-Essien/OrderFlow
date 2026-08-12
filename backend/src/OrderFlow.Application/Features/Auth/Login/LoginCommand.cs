using MediatR;
using OrderFlow.Shared.DTOs.Auth;

namespace OrderFlow.Application.Features.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
