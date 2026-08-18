using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

/// <summary>Stock movement audit table. <c>Type</c> is stored as a string with a CHECK of the enum set.</summary>
public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    /// <summary>Applies resulting-stock range and movement-type CHECK.</summary>
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements", table =>
        {
            table.HasCheckConstraint(
                "CK_StockMovements_ResultingStockRange",
                "\"ResultingStock\" >= 0 AND \"ResultingStock\" <= 99999999");
            table.HasCheckConstraint(
                "CK_StockMovements_Type",
                "\"Type\" IN ('Adjustment', 'Reserve', 'Deduct', 'Release')");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).HasMaxLength(26).IsRequired();
        builder.Property(m => m.ShopId).HasMaxLength(26).IsRequired();
        builder.Property(m => m.ProductId).HasMaxLength(26).IsRequired();
        builder.Property(m => m.QuantityDelta).IsRequired();
        builder.Property(m => m.ResultingStock).IsRequired();
        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(m => m.Notes).HasMaxLength(400);
        builder.Property(m => m.CreatedByUserId).HasMaxLength(26);
        builder.Property(m => m.CreatedAt).IsRequired();

        builder.HasIndex(m => m.ShopId);
        builder.HasIndex(m => m.ProductId);
        builder.HasIndex(m => new { m.ShopId, m.CreatedAt });

        builder.HasOne<OrderFlow.Domain.Entities.Shop>()
            .WithMany()
            .HasForeignKey(m => m.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<OrderFlow.Domain.Entities.User>()
            .WithMany()
            .HasForeignKey(m => m.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
