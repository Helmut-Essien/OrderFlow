using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

public sealed class ShopRepository(AppDbContext db) : IShopRepository
{
    public Task<Shop?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return db.Shops.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public Task<Shop?> GetByLicenseLookupHashAsync(
        string licenseLookupHash,
        CancellationToken cancellationToken = default)
    {
        return db.Shops.IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.LicenseLookupHash == licenseLookupHash, cancellationToken);
    }

    public void Add(Shop shop) => db.Shops.Add(shop);
}
