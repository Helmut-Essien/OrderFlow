using MediatR;
using OrderFlow.Shared.DTOs.Auth;

namespace OrderFlow.Application.Features.Auth.GetMe;

public sealed record GetMeQuery : IRequest<MeResponse>;
