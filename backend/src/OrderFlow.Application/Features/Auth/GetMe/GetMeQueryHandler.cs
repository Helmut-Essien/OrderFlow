using MediatR;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Features.Auth;
using OrderFlow.Domain;
using OrderFlow.Shared.DTOs.Auth;

namespace OrderFlow.Application.Features.Auth.GetMe;

/// <summary>Loads user + shop for <c>GET /api/auth/me</c>. Surfaces <c>PlanUnrecognized</c> from the shop snapshot.</summary>
public sealed class GetMeQueryHandler(
    ICurrentUser currentUser,
    IShopRepository shops,
    IUserRepository users) : IRequestHandler<GetMeQuery, MeResponse>
{
    /// <summary>Loads the JWT user and shop snapshot. Throws 401 when the user or shop is missing.</summary>
    public async Task<MeResponse> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            throw new UnauthorizedAppException("Not authenticated.");

        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new UnauthorizedAppException("User not found.");

        var shop = await shops.GetByIdAsync(user.ShopId, cancellationToken)
            ?? throw new NotFoundAppException("Shop not found.");

        var plan = PlanQuota.FromPlanName(shop.PlanName);
        if (shop.PlanUnrecognized)
            plan = plan with { IsUnrecognized = true, OriginalName = shop.PlanName };

        return AuthMapping.ToMeResponse(user, shop, plan);
    }
}
