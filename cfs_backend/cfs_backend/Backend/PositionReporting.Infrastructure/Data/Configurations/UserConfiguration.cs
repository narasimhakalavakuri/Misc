using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PositionReporting.Infrastructure.Data.Entities;

namespace PositionReporting.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("TBL_USER"); // Assuming legacy table name

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("uid")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.UserId)
            .HasColumnName("userid")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Email) // Adding an email field for SAML mapping
            .HasColumnName("email")
            .HasMaxLength(256);

        builder.Property(u => u.DepartmentId)
            .HasColumnName("dept")
            .HasMaxLength(10);

        builder.Property(u => u.AccessMask) // Delphi uses a string like "1110010"
            .HasColumnName("accessmask")
            .HasMaxLength(20);

        builder.Property(u => u.PasswordHash) // For local authentication (if needed)
            .HasColumnName("pwd")
            .HasMaxLength(256);

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();
            
        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(u => u.UserId)
            .IsUnique();
    }
}