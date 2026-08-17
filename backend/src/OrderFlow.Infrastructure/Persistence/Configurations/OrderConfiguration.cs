using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Order table: max lengths, status/source CHECKs, monthly-cap and dashboard indexes, <c>Version</c> as a concurrency token.
/// </summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <summary>Applies CHECKs, indexes, and the shop restrict FK. Lines cascade with the order.</summary>
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
        {
            table.HasCheckConstraint(
                "CK_Orders_CustomerNameNotEmpty",
                "char_length(btrim(\"CustomerName\")) > 0");
            table.HasCheckConstraint(
                "CK_Orders_Status",
                "\"Status\" IN ('Pending', 'Confirmed', 'Paid', 'Fulfilled', 'Cancelled')");
            table.HasCheckConstraint(
                "CK_Orders_Source",
                "\"Source\" IN ('Manual', 'WhatsApp')");
            table.HasCheckConstraint(
                "CK_Orders_TotalAmountNonNegative",
                "\"TotalAmount\" >= 0");
            table.HasCheckConstraint(
                "CK_Orders_VersionPositive",
                "\"Version\" >= 1");
        });

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).HasMaxLength(26).IsRequired();
        builder.Property(o => o.ShopId).HasMaxLength(26).IsRequired();
        builder.Property(o => o.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(o => o.CustomerPhone).HasMaxLength(50);
        builder.Property(o => o.Notes).HasMaxLength(400);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(o => o.Source).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(o => o.NeedsClarification).IsRequired();
        builder.Property(o => o.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(o => o.Version).IsConcurrencyToken().IsRequired();
        builder.Property(o => o.CreatedByUserId).HasMaxLength(26);
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired();

        builder.HasIndex(o => o.ShopId);
        // List newest-first and monthly plan-cap counts filter CreatedAt per shop.
        builder.HasIndex(o => new { o.ShopId, o.CreatedAt });
        builder.HasIndex(o => new { o.ShopId, o.Status });
        // Dashboard “today’s sales” filters PaidAt per shop, then keeps only Paid/Fulfilled.
        builder.HasIndex(o => new { o.ShopId, o.PaidAt });

        builder.HasOne(o => o.Shop)
            .WithMany()
            .HasForeignKey(o => o.ShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Lines)
            .WithOne(l => l.Order)
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
