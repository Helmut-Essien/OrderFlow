using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// Shop table. License lookup hash is unique and exactly 64 chars; plaintext keys are never stored.
/// </summary>
public class ShopConfiguration : IEntityTypeConfiguration<Shop>
{
    /// <summary>Applies license-hash uniqueness and plan snapshot columns.</summary>
    public void Configure(EntityTypeBuilder<Shop> builder)
    {
        builder.ToTable("Shops", table =>
        {
            table.HasCheckConstraint(
                "CK_Shops_NameNotEmpty",
                "char_length(btrim(\"Name\")) > 0");
            table.HasCheckConstraint(
                "CK_Shops_LicenseLookupHashLength",
                "char_length(\"LicenseLookupHash\") = 64");
            table.HasCheckConstraint(
                "CK_Shops_PlanNameNotEmpty",
                "char_length(btrim(\"PlanName\")) > 0");
            table.HasCheckConstraint(
                "CK_Shops_WhatsAppConnectionStatus",
                "\"WhatsAppConnectionStatus\" IN ('Disconnected', 'Connected', 'Error')");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasMaxLength(26).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Phone).HasMaxLength(50);
        builder.Property(s => s.Address).HasMaxLength(400);
        builder.Property(s => s.LicenseLookupHash).HasMaxLength(64).IsRequired();
        builder.Property(s => s.ProtectedLicenseKey).HasMaxLength(4000).IsRequired();
        builder.Property(s => s.PlanName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.WhatsAppConnectionStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.HasIndex(s => s.LicenseLookupHash).IsUnique();

        builder.HasMany(s => s.Users)
            .WithOne(u => u.Shop)
            .HasForeignKey(u => u.ShopId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
