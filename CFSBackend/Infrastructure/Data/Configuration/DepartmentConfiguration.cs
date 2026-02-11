using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectName.Infrastructure.Data.Entities;

namespace ProjectName.Infrastructure.Data.Configuration
{
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.ToTable("departments");

            builder.HasKey(d => d.DeptCode);
            builder.Property(d => d.DeptCode)
                .HasColumnName("dept_code")
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(d => d.DeptDesc)
                .HasColumnName("dept_desc")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(d => d.ApprType)
                .HasColumnName("appr_type")
                .HasMaxLength(1)
                .IsRequired(); // e.g. 'N' for normal

            builder.Property(d => d.RefNo)
                .HasColumnName("ref_no")
                .HasDefaultValue(0)
                .IsRequired(); // For sequence generation

            builder.Property(d => d.RefLock)
                .HasColumnName("ref_lock")
                .HasMaxLength(50)
                .HasDefaultValue(".")
                .IsRequired(); // Lock for reference number generation

            builder.Property(d => d.ClosedDate)
                .HasColumnName("closed_date"); // Nullable if not closed

            builder.Property(d => d.ClosedBy)
                .HasColumnName("closed_by")
                .HasMaxLength(100);

            builder.Property(d => d.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            builder.Property(d => d.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            // Index for performance if querying by closed_date often
            builder.HasIndex(d => d.ClosedDate);
        }
    }
}