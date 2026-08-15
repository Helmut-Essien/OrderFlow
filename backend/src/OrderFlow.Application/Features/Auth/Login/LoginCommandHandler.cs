using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Features.Auth;
using OrderFlow.Domain;
using OrderFlow.Shared.DTOs.Auth;

namespace OrderFlow.Application.Features.Auth.Login;

/// <summary>
/// Authenticates by email/password and issues an OrderFlow JWT. Failed lookups use the same message as bad passwords.
/// </summary>
public sealed class LoginCommandHandler(
    IShopRepository shops,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwt) : IRequestHandler<LoginCommand, AuthResponse>
{
    /// <summary>
    /// BCrypt hash of a throwaway secret (work factor 11). Used when the email is unknown so verify cost matches a real user.
    /// </summary>
    private const string DummyPasswordHash = "$2a$11$bePeA9FXpCtiTvP7RSzQCOyAiGn7sNxR97CyB.FjcI.wJM7RYG8Ty";

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(email, cancellationToken);
        // Always Verify so unknown emails take the same time as a wrong password (no user-enumeration via timing).
        var passwordMatches = passwordHasher.Verify(request.Password, user?.PasswordHash ?? DummyPasswordHash);
        if (user is null || !passwordMatches)
            throw new UnauthorizedAppException("Invalid email or password.");

        var shop = await shops.GetByIdAsync(user.ShopId, cancellationToken)
            ?? throw new UnauthorizedAppException("Shop not found.");

        var plan = PlanQuota.FromPlanName(shop.PlanName);
        if (shop.PlanUnrecognized)
            plan = plan with { IsUnrecognized = true, OriginalName = shop.PlanName };

        var token = jwt.Create(user, shop, plan);
        return AuthMapping.ToAuthResponse(token, user, shop, plan);
    }
}
