using MediatR;
using OrderFlow.Shared.DTOs.Auth;

namespace OrderFlow.Application.Features.Auth.GetMe;

/// <summary>Returns the authenticated user, shop, and plan snapshot from the JWT session.</summary>
public sealed record GetMeQuery : IRequest<MeResponse>;
