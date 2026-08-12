using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Features.Auth;
using OrderFlow.Domain;
using OrderFlow.Shared.DTOs.Auth;

namespace OrderFlow.Application.Features.Auth.Login;

public sealed class LoginCommandHandler(
    IShopRepository shops,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwt) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(email, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
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
