using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Common.Interfaces;

/// <summary>Persistence port for stock movement audit rows.</summary>
public interface IStockMovementRepository
{
    void Add(StockMovement movement);
}
