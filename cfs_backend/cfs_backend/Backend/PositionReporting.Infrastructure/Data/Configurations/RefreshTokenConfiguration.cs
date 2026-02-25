using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PositionReporting.Infrastructure.Data.Entities;

namespace PositionReporting.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("TBL_REFRESH_TOKEN"); // Centralized store table name

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Id)
            .ValueGeneratedOnAdd(); // Auto-generate GUID

        builder.Property(rt => rt.TokenHash)
            .HasMaxLength(256) // Sufficient for SHA256 base64 string
            .IsRequired();

        builder.Property(rt => rt.UserId)
            .IsRequired();

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired();

        builder.Property(rt => rt.CreatedAt)
            .IsRequired();

        builder.Property(rt => rt.IsRevoked)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(rt => rt.ReplacedByTokenHash)
            .HasMaxLength(256);

        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique(); // Ensure no duplicate token hashes

        builder.HasIndex(rt => rt.UserId); // For efficient lookup by user

        // One-to-many relationship with User
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete if user is removed
    }
}