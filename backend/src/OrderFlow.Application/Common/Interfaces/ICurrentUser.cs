namespace OrderFlow.Application.Common.Interfaces;

public interface ICurrentUser
{
    string? UserId { get; }

    string? ShopId { get; }

    string? Role { get; }

    bool IsAuthenticated { get; }
}
