using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

/// <summary>User persistence. Email lookup ignores tenant filters because login is anonymous and email is globally unique.</summary>
public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Login is anonymous; email is unique across shops so the tenant filter must not hide the row.
        return db.Users.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public void Add(User user) => db.Users.Add(user);
}
