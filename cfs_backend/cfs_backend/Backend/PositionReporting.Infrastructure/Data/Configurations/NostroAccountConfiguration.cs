using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PositionReporting.Infrastructure.Data.Entities;

namespace PositionReporting.Infrastructure.Data.Configurations;

public class NostroAccountConfiguration : IEntityTypeConfiguration<NostroAccount>
{
    public void Configure(EntityTypeBuilder<NostroAccount> builder)
    {
        builder.ToTable("TBL_MISC"); // Assuming Nostro accounts are stored in a generic TBL_MISC
                                      // data01 from TBL_MISC (accountNumber)
                                      // dataid1 = 'NOSTRO', dataid2 = '[CURRENCY]'

        builder.HasKey(n => new { n.AccountNumber, n.CurrencyCode }); // Composite key

        builder.Property(n => n.AccountNumber)
            .HasColumnName("data01")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(n => n.CurrencyCode)
            .HasColumnName("dataid2") // Used to filter by currency
            .HasMaxLength(3)
            .IsRequired();

        // Additional properties for filtering in TBL_MISC
        builder.Property(n => n.DataId1) // For 'NOSTRO' constant
            .HasColumnName("dataid1")
            .HasMaxLength(50)
            .HasDefaultValue("NOSTRO")
            .IsRequired();
            
        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(n => n.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.HasQueryFilter(n => n.DataId1 == "NOSTRO"); // Ensure only Nostro accounts are queried
    }
}