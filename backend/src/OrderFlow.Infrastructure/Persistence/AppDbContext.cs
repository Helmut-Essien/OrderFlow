using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL context. Global query filters scope Shop/User/Product/StockMovement/Order/OrderLine to the JWT <c>shopId</c> when present.
/// </summary>
public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUser currentUser) : DbContext(options)
{
    /// <summary>Tenant shops. Filtered to JWT shop id when authenticated.</summary>
    public DbSet<Shop> Shops => Set<Shop>();

    /// <summary>Shop staff. Email lookup at login bypasses the shop filter.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Catalog products. Stock mutations use raw SQL, not tracked updates.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>Immutable stock audit rows.</summary>
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    /// <summary>Shop orders. Status mutations are tracked; list/dashboard reads are untracked projections.</summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>Order line snapshots. Filtered by shop independently of the parent include.</summary>
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    /// <inheritdoc />
    /// <remarks>
    /// Applies entity configurations and tenant query filters. Anonymous/design-time (<c>ShopId</c> null) disables the filters so signup can insert the first shop.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Null ShopId (design-time / anonymous) disables the filter so signup can insert the first shop.
        modelBuilder.Entity<Shop>()
            .HasQueryFilter(s => currentUser.ShopId == null || s.Id == currentUser.ShopId);

        modelBuilder.Entity<User>()
            .HasQueryFilter(u => currentUser.ShopId == null || u.ShopId == currentUser.ShopId);

        modelBuilder.Entity<Product>()
            .HasQueryFilter(p => currentUser.ShopId == null || p.ShopId == currentUser.ShopId);

        modelBuilder.Entity<StockMovement>()
            .HasQueryFilter(m => currentUser.ShopId == null || m.ShopId == currentUser.ShopId);

        modelBuilder.Entity<Order>()
            .HasQueryFilter(o => currentUser.ShopId == null || o.ShopId == currentUser.ShopId);

        modelBuilder.Entity<OrderLine>()
            .HasQueryFilter(l => currentUser.ShopId == null || l.ShopId == currentUser.ShopId);
    }
}
