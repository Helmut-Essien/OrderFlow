using OrderFlow.Application.Common.Interfaces;

namespace OrderFlow.Infrastructure.Persistence;

internal sealed class NullCurrentUser : ICurrentUser
{
    public string? UserId => null;

    public string? ShopId => null;

    public string? Role => null;

    public bool IsAuthenticated => false;
}
