using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderFlow.Application.Common.Exceptions;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Commits EF changes. Maps <see cref="DbUpdateConcurrencyException"/> to <see cref="ConcurrencyAppException"/>
/// and PostgreSQL unique-violation (23505) to <see cref="ConflictAppException"/> so concurrent
/// signup/SKU races produce 409 instead of 500.
/// </summary>
public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyAppException(
                "This record was updated by someone else. Refresh and try again.");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictAppException(
                "A record with the same unique value already exists.");
        }
    }

    /// <inheritdoc />
    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> work,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await work(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>PostgreSQL error code 23505 is a unique-constraint violation.</summary>
    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
