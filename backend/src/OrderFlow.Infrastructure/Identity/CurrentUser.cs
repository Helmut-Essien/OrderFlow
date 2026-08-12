using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Shared.Constants;

namespace OrderFlow.Infrastructure.Identity;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId =>
        accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? accessor.HttpContext?.User.FindFirstValue("sub");

    public string? ShopId => accessor.HttpContext?.User.FindFirstValue(ClaimNames.ShopId);

    public string? Role => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
