using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Common.Interfaces;

/// <summary>Persistence port for shops (tenants).</summary>
public interface IShopRepository
{
    Task<Shop?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Finds a shop by SHA-256 license lookup hash. Used at signup to reject duplicate keys.</summary>
    Task<Shop?> GetByLicenseLookupHashAsync(string licenseLookupHash, CancellationToken cancellationToken = default);

    void Add(Shop shop);
}
