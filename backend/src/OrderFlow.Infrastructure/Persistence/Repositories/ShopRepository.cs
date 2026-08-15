using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

/// <summary>Shop persistence. License-hash lookup ignores tenant filters so signup can detect an already-registered key.</summary>
public sealed class ShopRepository(AppDbContext db) : IShopRepository
{
    public Task<Shop?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return db.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public Task<Shop?> GetByLicenseLookupHashAsync(
        string licenseLookupHash,
        CancellationToken cancellationToken = default)
    {
        // Signup is anonymous, so the shop filter would hide every row without this bypass.
        return db.Shops.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.LicenseLookupHash == licenseLookupHash, cancellationToken);
    }

    public void Add(Shop shop) => db.Shops.Add(shop);
}
