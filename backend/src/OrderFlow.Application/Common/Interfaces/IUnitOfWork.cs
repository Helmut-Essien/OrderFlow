namespace OrderFlow.Application.Common.Interfaces;

/// <summary>Commits the current EF change tracker. Call once per use-case after all mutations.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
