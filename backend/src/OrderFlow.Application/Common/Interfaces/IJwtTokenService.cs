using OrderFlow.Domain;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Common.Interfaces;

public sealed record JwtTokenResult(string Token, DateTime ExpiresAt);

public interface IJwtTokenService
{
    JwtTokenResult Create(User user, Shop shop, PlanQuota plan);
}
