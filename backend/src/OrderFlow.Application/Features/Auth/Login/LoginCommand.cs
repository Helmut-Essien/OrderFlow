using MediatR;
using OrderFlow.Shared.DTOs.Auth;

namespace OrderFlow.Application.Features.Auth.Login;

/// <summary>Email/password login. License keys are not accepted here.</summary>
public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;
