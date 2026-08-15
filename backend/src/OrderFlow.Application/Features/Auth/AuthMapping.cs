using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;
using OrderFlow.Shared.DTOs.Auth;

namespace OrderFlow.Application.Features.Auth;

/// <summary>Maps domain user/shop/plan to public auth DTOs inside handlers, never in the controller.</summary>
internal static class AuthMapping
{
    public static AuthResponse ToAuthResponse(JwtTokenResult token, User user, Shop shop, PlanQuota plan)
    {
        return new AuthResponse
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            ShopId = shop.Id,
            ShopName = shop.Name,
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString(),
            Plan = ToPlanInfo(shop, plan)
        };
    }

    public static MeResponse ToMeResponse(User user, Shop shop, PlanQuota plan)
    {
        return new MeResponse
        {
            ShopId = shop.Id,
            ShopName = shop.Name,
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString(),
            Plan = ToPlanInfo(shop, plan)
        };
    }

    private static PlanInfoDto ToPlanInfo(Shop shop, PlanQuota plan)
    {
        return new PlanInfoDto
        {
            Name = plan.Name,
            OriginalName = plan.OriginalName ?? shop.PlanName,
            IsUnrecognized = plan.IsUnrecognized,
            MaxProducts = plan.MaxProducts,
            MaxOrdersPerMonth = plan.MaxOrdersPerMonth,
            MaxUsers = plan.MaxUsers,
            AiFeatures = plan.AiFeatures,
            ExpiresAt = shop.PlanExpiresAt
        };
    }
}
