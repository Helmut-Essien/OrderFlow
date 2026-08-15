using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrderFlow.Application.Features.Auth.GetMe;
using OrderFlow.Application.Features.Auth.Login;
using OrderFlow.Application.Features.Auth.SignUp;
using OrderFlow.Shared.DTOs.Auth;

namespace OrderFlow.Api.Controllers;

/// <summary>
/// Shop signup, login, and session. Rate-limited to 20 requests/minute. License keys are accepted only on signup.
/// </summary>
[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>Registers a shop from a Platform license key and returns an OrderFlow JWT.</summary>
    /// <response code="401">License is invalid.</response>
    /// <response code="409">License or email already registered.</response>
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

    /// <summary>Authenticates with email and password. Does not accept a license key.</summary>
    /// <response code="401">Invalid email or password.</response>
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

    /// <summary>Returns the current user, shop, and plan snapshot for the JWT.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMeQuery(), cancellationToken);
        return Ok(result);
    }
}
