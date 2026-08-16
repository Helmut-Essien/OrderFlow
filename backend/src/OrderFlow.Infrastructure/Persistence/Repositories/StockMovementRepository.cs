using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

/// <summary>Adds stock movement audit rows to the change tracker.</summary>
public sealed class StockMovementRepository(AppDbContext db) : IStockMovementRepository
{
    /// <inheritdoc />
    public void Add(StockMovement movement) => db.StockMovements.Add(movement);
}
