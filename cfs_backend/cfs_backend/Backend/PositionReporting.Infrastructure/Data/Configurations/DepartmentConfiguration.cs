using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PositionReporting.Infrastructure.Data.Entities;

namespace PositionReporting.Infrastructure.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("DEPTLIST"); // Assuming legacy table name

        builder.HasKey(d => d.DepartmentCode);

        builder.Property(d => d.DepartmentCode)
            .HasColumnName("deptcode")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(d => d.DepartmentDescription)
            .HasColumnName("deptdesc")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.ClosedDate)
            .HasColumnName("closed_date"); // DateTimeOffset or DateTime?

        builder.Property(d => d.ClosedBy)
            .HasColumnName("closed_by")
            .HasMaxLength(100);

        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasMaxLength(10)
            .IsRequired(); // OPEN or CLOSED

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at");
        
        // Add default value for ref_lock based on Delphi ('.')
        builder.Property<string>("RefLock") // Shadow property for 'REF_LOCK'
            .HasColumnName("REF_LOCK")
            .HasMaxLength(1)
            .HasDefaultValue(".");
    }
}