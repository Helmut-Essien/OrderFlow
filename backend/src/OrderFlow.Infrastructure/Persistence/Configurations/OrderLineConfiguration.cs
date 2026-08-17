using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

/// <summary>Order line snapshots. Quantity and money CHECKs match <c>OrderConstraints</c> / <c>ProductConstraints</c>.</summary>
public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    /// <summary>Applies quantity/price CHECKs and restrict FKs to shop and product.</summary>
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines", table =>
        {
            table.HasCheckConstraint(
                "CK_OrderLines_ProductNameNotEmpty",
                "char_length(btrim(\"ProductName\")) > 0");
            table.HasCheckConstraint(
                "CK_OrderLines_SkuNotEmpty",
                "char_length(btrim(\"Sku\")) > 0");
            table.HasCheckConstraint(
                "CK_OrderLines_QuantityRange",
                "\"Quantity\" >= 1 AND \"Quantity\" <= 99999999");
            table.HasCheckConstraint(
                "CK_OrderLines_UnitPriceNonNegative",
                "\"UnitPrice\" >= 0 AND \"UnitPrice\" <= 999999999.99");
            table.HasCheckConstraint(
                "CK_OrderLines_LineTotalNonNegative",
                "\"LineTotal\" >= 0");
        });

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).HasMaxLength(26).IsRequired();
        builder.Property(l => l.OrderId).HasMaxLength(26).IsRequired();
        builder.Property(l => l.ShopId).HasMaxLength(26).IsRequired();
        builder.Property(l => l.ProductId).HasMaxLength(26).IsRequired();
        builder.Property(l => l.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Sku).HasMaxLength(50).IsRequired();
        builder.Property(l => l.Quantity).IsRequired();
        builder.Property(l => l.UnitPrice).HasPrecision(12, 2).IsRequired();
        builder.Property(l => l.LineTotal).HasPrecision(18, 2).IsRequired();

        builder.HasIndex(l => l.OrderId);
        builder.HasIndex(l => l.ShopId);
        builder.HasIndex(l => new { l.OrderId, l.ProductId }).IsUnique();

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
