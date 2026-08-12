using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Common.Interfaces;

public interface IShopRepository
{
    Task<Shop?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<Shop?> GetByLicenseLookupHashAsync(string licenseLookupHash, CancellationToken cancellationToken = default);

    void Add(Shop shop);
}
