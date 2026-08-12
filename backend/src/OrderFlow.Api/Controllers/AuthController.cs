using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrderFlow.Application.Features.Auth.GetMe;
using OrderFlow.Application.Features.Auth.Login;
using OrderFlow.Application.Features.Auth.SignUp;
using OrderFlow.Shared.DTOs.Auth;

namespace OrderFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> SignUp(
        [FromBody] SignUpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SignUpCommand(
                request.LicenseKey,
                request.Email,
                request.Password,
                request.ShopName,
                request.DisplayName,
                request.Phone),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMeQuery(), cancellationToken);
        return Ok(result);
    }
}
