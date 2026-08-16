using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Common.Interfaces;

/// <summary>Persistence port for stock movement audit rows.</summary>
public interface IStockMovementRepository
{
    /// <summary>Stages an audit row. Call inside the same transaction as the stock UPDATE.</summary>
    void Add(StockMovement movement);
}
