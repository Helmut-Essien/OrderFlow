using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Common.Interfaces;

/// <summary>Persistence port for shop users. Email lookup is case-insensitive (stored lowercase).</summary>
public interface IUserRepository
{
    /// <summary>Untracked read. User rows are not mutated after insert in current slices.</summary>
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Looks up by already-normalized (lowercase) email.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    void Add(User user);
}
