using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public class ShopConfiguration : IEntityTypeConfiguration<Shop>
{
    public void Configure(EntityTypeBuilder<Shop> builder)
    {
        builder.ToTable("Shops");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasMaxLength(26).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Phone).HasMaxLength(50);
        builder.Property(s => s.Address).HasMaxLength(400);
        builder.Property(s => s.LicenseLookupHash).HasMaxLength(64).IsRequired();
        builder.Property(s => s.ProtectedLicenseKey).HasMaxLength(4000).IsRequired();
        builder.Property(s => s.PlanName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.WhatsAppConnectionStatus).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(s => s.LicenseLookupHash).IsUnique();

        builder.HasMany(s => s.Users)
            .WithOne(u => u.Shop)
            .HasForeignKey(u => u.ShopId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
