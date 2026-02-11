using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectName.Infrastructure.Data.Entities;

namespace ProjectName.Infrastructure.Data.Configuration
{
    public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
    {
        public void Configure(EntityTypeBuilder<Currency> builder)
        {
            builder.ToTable("currencies");

            builder.HasKey(c => c.CurrCode);
            builder.Property(c => c.CurrCode)
                .HasColumnName("curr_code")
                .HasMaxLength(3)
                .IsRequired();

            builder.Property(c => c.CurrDesc)
                .HasColumnName("curr_desc")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(c => c.Deciml)
                .HasColumnName("deciml")
                .IsRequired();

            builder.Property(c => c.Tts)
                .HasColumnName("tts")
                .HasColumnType("numeric(18,4)")
                .IsRequired();

            builder.Property(c => c.Sts)
                .HasColumnName("sts")
                .HasColumnType("numeric(18,4)")
                .IsRequired();

            builder.Property(c => c.Bts)
                .HasColumnName("bts")
                .HasColumnType("numeric(18,4)")
                .IsRequired();

            builder.Property(c => c.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();

            builder.Property(c => c.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .IsRequired();
        }
    }
}