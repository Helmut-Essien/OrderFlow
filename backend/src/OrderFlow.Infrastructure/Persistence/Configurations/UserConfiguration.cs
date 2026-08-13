using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", table =>
        {
            table.HasCheckConstraint(
                "CK_Users_EmailNotEmpty",
                "char_length(btrim(\"Email\")) > 0");
            table.HasCheckConstraint(
                "CK_Users_DisplayNameNotEmpty",
                "char_length(btrim(\"DisplayName\")) > 0");
            table.HasCheckConstraint(
                "CK_Users_PasswordHashNotEmpty",
                "char_length(btrim(\"PasswordHash\")) > 0");
            table.HasCheckConstraint(
                "CK_Users_Role",
                "\"Role\" IN ('Owner', 'Assistant')");
        });

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasMaxLength(26).IsRequired();
        builder.Property(u => u.ShopId).HasMaxLength(26).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(320).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.ShopId);
    }
}
