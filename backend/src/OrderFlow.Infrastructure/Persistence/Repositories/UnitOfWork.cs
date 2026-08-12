using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await db.SaveChangesAsync(cancellationToken);
    }
}
