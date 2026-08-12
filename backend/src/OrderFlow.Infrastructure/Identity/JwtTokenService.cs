using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain;
using OrderFlow.Domain.Entities;
using OrderFlow.Shared.Constants;

namespace OrderFlow.Infrastructure.Identity;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    public JwtTokenResult Create(User user, Shop shop, PlanQuota plan)
    {
        var settings = options.Value;
        var expiresAt = DateTime.UtcNow.AddMinutes(settings.ExpiryMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Id,
            [JwtRegisteredClaimNames.Email] = user.Email,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            [ClaimTypes.NameIdentifier] = user.Id,
            [ClaimTypes.Name] = user.DisplayName,
            [ClaimTypes.Role] = user.Role.ToString(),
            [ClaimNames.ShopId] = shop.Id,
            [ClaimNames.PlanName] = plan.Name
        };

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Claims = claims,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        });

        return new JwtTokenResult(token, expiresAt);
    }
}
