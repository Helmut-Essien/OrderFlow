using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Infrastructure.Persistence;

/// <summary>Anonymous principal for EF design-time factories so global shop filters stay disabled.</summary>
internal sealed class NullCurrentUser : ICurrentUser
{
    public string? UserId => null;

    public string? ShopId => null;

    public string? Role => null;

    public bool IsAuthenticated => false;
}
