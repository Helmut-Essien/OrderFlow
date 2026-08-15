namespace OrderFlow.Application.Common.Interfaces;

/// <summary>Commits the current EF change tracker. Call once per use-case after all mutations.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="work"/> in a database transaction so raw SQL and tracked inserts commit or roll back together.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default);
}
