using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUser currentUser) : DbContext(options)
{
    public DbSet<Shop> Shops => Set<Shop>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<Shop>()
            .HasQueryFilter(s => currentUser.ShopId == null || s.Id == currentUser.ShopId);

        modelBuilder.Entity<User>()
            .HasQueryFilter(u => currentUser.ShopId == null || u.ShopId == currentUser.ShopId);
    }
}
