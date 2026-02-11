using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectName.Infrastructure.Data.Entities;

namespace ProjectName.Infrastructure.Data.Configuration
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");

            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id)
                .HasColumnName("uid")
                .HasDefaultValueSql("gen_random_uuid()")
                .IsRequired();

            builder.Property(u => u.UserId)
                .HasColumnName("user_id")
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(u => u.UserId).IsUnique(); // Ensure UserIds are unique

            builder.Property(u => u.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(255) // BCrypt hashes are typically ~60-72 chars, 255 is safe
                .IsRequired();

            builder.Property(u => u.Department)
                .HasColumnName("dept")
                .HasMaxLength(10); // Optional, can be null for admin users without specific dept

            builder.Property(u => u.AccessMask)
                .HasColumnName("access_mask")
                .HasMaxLength(30) // Long enough for future expansion
                .IsRequired();

            builder.Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            builder.Property(u => u.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            // Establish foreign key relationship with Department (optional, if enforcing referential integrity)
            // This would require a Department entity.
            builder.HasOne<Department>()
                .WithMany()
                .HasForeignKey(u => u.Department)
                .HasPrincipalKey(d => d.DeptCode)
                .OnDelete(DeleteBehavior.SetNull); // If department is deleted, set user's dept to null
        }
    }
}