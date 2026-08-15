using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

/// <summary>Commits EF changes. Maps <see cref="DbUpdateConcurrencyException"/> to <see cref="ConcurrencyAppException"/>.</summary>
public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyAppException(
                "This product was updated by someone else. Refresh and try again.");
        }
    }
}
