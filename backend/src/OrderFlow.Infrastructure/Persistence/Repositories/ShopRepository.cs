using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Infrastructure.Persistence.Repositories;

/// <summary>Shop persistence. License-hash lookup ignores tenant filters so signup can detect an already-registered key.</summary>
public sealed class ShopRepository(AppDbContext db) : IShopRepository
{
    /// <inheritdoc />
    public Task<Shop?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return db.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task AcquirePlanCapLockAsync(string shopId, CancellationToken cancellationToken = default)
    {
        // Advisory lock (not FOR UPDATE on a subquery) so two creates cannot both pass the cap count.
        var key = PlanCapLockKey(shopId);
        return db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }

    /// <summary>Stable bigint from shop id so every create for that tenant shares one lock.</summary>
    private static long PlanCapLockKey(string shopId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(shopId));
        return BitConverter.ToInt64(hash, 0);
    }

    /// <inheritdoc />
    public Task<Shop?> GetByLicenseLookupHashAsync(
        string licenseLookupHash,
        CancellationToken cancellationToken = default)
    {
        // Signup is anonymous, so the shop filter would hide every row without this bypass.
        return db.Shops.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.LicenseLookupHash == licenseLookupHash, cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Shop shop) => db.Shops.Add(shop);
}
