using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public void Add(User user) => db.Users.Add(user);
}
