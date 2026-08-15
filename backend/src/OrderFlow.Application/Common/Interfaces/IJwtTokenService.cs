using OrderFlow.Domain;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Common.Interfaces;

/// <summary>Issued OrderFlow JWT (not a Platform token) plus UTC expiry.</summary>
public sealed record JwtTokenResult(string Token, DateTime ExpiresAt);

/// <summary>Creates shop-scoped JWTs with <c>sub</c>, <c>shopId</c>, <c>role</c>, and <c>planName</c> claims.</summary>
public interface IJwtTokenService
{
    JwtTokenResult Create(User user, Shop shop, PlanQuota plan);
}
