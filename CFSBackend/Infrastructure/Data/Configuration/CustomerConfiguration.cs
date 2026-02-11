using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectName.Infrastructure.Data.Entities;

namespace ProjectName.Infrastructure.Data.Configuration
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("customers");

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()")
                .IsRequired();

            builder.Property(c => c.AcctNo)
                .HasColumnName("acct_no")
                .HasMaxLength(20)
                .IsRequired();

            builder.HasIndex(c => c.AcctNo).IsUnique();

            builder.Property(c => c.AbbrvName)
                .HasColumnName("abbrv_name")
                .HasMaxLength(50);

            builder.Property(c => c.CustName1)
                .HasColumnName("cust_name1")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(c => c.HomeCurrency)
                .HasColumnName("home_currency")
                .HasMaxLength(3); // Assuming this is also stored in custfile
        }
    }
}