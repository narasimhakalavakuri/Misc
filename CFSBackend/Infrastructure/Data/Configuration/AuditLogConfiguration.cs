using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectName.Infrastructure.Data.Entities;

namespace ProjectName.Infrastructure.Data.Configuration
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("audit_logs");

            builder.HasKey(al => al.Id);
            builder.Property(al => al.Id)
                .HasColumnName("log_id")
                .HasDefaultValueSql("gen_random_uuid()")
                .IsRequired();

            builder.Property(al => al.UserId)
                .HasColumnName("user_id")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(al => al.LogStr)
                .HasColumnName("logstr")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(al => al.LogTime)
                .HasColumnName("log_time")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
        }
    }
}