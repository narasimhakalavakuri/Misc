using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PositionReporting.Infrastructure.Data.Entities;

namespace PositionReporting.Infrastructure.Data.Configurations;

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("TBL_CURRENCY"); // Assuming legacy table name

        builder.HasKey(c => c.CurrencyCode);

        builder.Property(c => c.CurrencyCode)
            .HasColumnName("curr_code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(c => c.DecimalPrecision)
            .HasColumnName("deciml")
            .IsRequired();

        builder.Property(c => c.ExchangeRateTTS)
            .HasColumnName("tts")
            .IsRequired();
            
        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");
    }
}