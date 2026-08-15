using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Product table: max lengths, numeric CHECKs, unique SKU per shop, and <c>Version</c> as a concurrency token. <c>IsLowStock</c> is ignored (computed).
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            table.HasCheckConstraint(
                "CK_Products_NameNotEmpty",
                "char_length(btrim(\"Name\")) > 0");
            table.HasCheckConstraint(
                "CK_Products_SkuNotEmpty",
                "char_length(btrim(\"Sku\")) > 0");
            table.HasCheckConstraint(
                "CK_Products_PriceNonNegative",
                "\"Price\" >= 0 AND \"Price\" <= 999999999.99");
            table.HasCheckConstraint(
                "CK_Products_StockRange",
                "\"Stock\" >= 0 AND \"Stock\" <= 99999999");
            table.HasCheckConstraint(
                "CK_Products_LowStockThresholdRange",
                "\"LowStockThreshold\" >= 0 AND \"LowStockThreshold\" <= 99999999");
            table.HasCheckConstraint(
                "CK_Products_VersionPositive",
                "\"Version\" >= 1");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasMaxLength(26).IsRequired();
        builder.Property(p => p.ShopId).HasMaxLength(26).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Sku).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Category).HasMaxLength(80);
        builder.Property(p => p.Price).HasPrecision(12, 2).IsRequired();
        builder.Property(p => p.Stock).IsRequired();
        builder.Property(p => p.LowStockThreshold).IsRequired();
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.Version).IsConcurrencyToken().IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => new { p.ShopId, p.Sku }).IsUnique();
        builder.HasIndex(p => p.ShopId);
        builder.HasIndex(p => new { p.ShopId, p.Category });
        // Dashboard low-stock and plan-cap counts filter active rows per shop.
        builder.HasIndex(p => new { p.ShopId, p.IsActive });

        builder.HasOne(p => p.Shop)
            .WithMany()
            .HasForeignKey(p => p.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.StockMovements)
            .WithOne(m => m.Product)
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(p => p.IsLowStock);
    }
}
