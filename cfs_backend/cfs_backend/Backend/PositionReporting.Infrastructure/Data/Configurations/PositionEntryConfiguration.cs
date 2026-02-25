using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PositionReporting.Infrastructure.Data.Entities;

namespace PositionReporting.Infrastructure.Data.Configurations;

public class PositionEntryConfiguration : IEntityTypeConfiguration<PositionEntry>
{
    public void Configure(EntityTypeBuilder<PositionEntry> builder)
    {
        builder.ToTable("MAINTABLE"); // Assuming legacy table name

        builder.HasKey(pe => pe.Id);

        builder.Property(pe => pe.Id)
            .HasColumnName("uid")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(pe => pe.DepartmentId)
            .HasColumnName("dept")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(pe => pe.TransactionType)
            .HasColumnName("ttype")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(pe => pe.ReferenceNumber)
            .HasColumnName("posref")
            .HasMaxLength(20);

        builder.Property(pe => pe.EntryDate)
            .HasColumnName("trans_date");

        builder.Property(pe => pe.ValueDate)
            .HasColumnName("value_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(pe => pe.IssueDate)
            .HasColumnName("issue_date");

        builder.Property(pe => pe.TheirReference)
            .HasColumnName("their_ref")
            .HasMaxLength(200);

        builder.Property(pe => pe.Reference)
            .HasColumnName("reference")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(pe => pe.DebitAccount)
            .HasColumnName("dr_acct")
            .HasMaxLength(50);

        builder.Property(pe => pe.DebitAccountName)
            .HasColumnName("dr_acctname")
            .HasMaxLength(100);

        builder.Property(pe => pe.DebitCurrency)
            .HasColumnName("dr_cur")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(pe => pe.DebitAmount)
            .HasColumnName("dr_amount")
            .HasColumnType("float")
            .IsRequired();

        builder.Property(pe => pe.CreditAccount)
            .HasColumnName("cr_acct")
            .HasMaxLength(50);

        builder.Property(pe => pe.CreditAccountName)
            .HasColumnName("cr_acctname")
            .HasMaxLength(100);

        builder.Property(pe => pe.CreditCurrency)
            .HasColumnName("cr_cur")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(pe => pe.CreditAmount)
            .HasColumnName("cr_amount")
            .HasColumnType("float")
            .IsRequired();

        builder.Property(pe => pe.CalculationSymbol)
            .HasColumnName("calc")
            .HasMaxLength(1)
            .IsRequired();

        builder.Property(pe => pe.ExchangeRate)
            .HasColumnName("rate")
            .HasColumnType("float")
            .IsRequired();

        builder.Property(pe => pe.Status)
            .HasColumnName("status")
            .HasMaxLength(1)
            .IsRequired();

        builder.Property(pe => pe.MakerId)
            .HasColumnName("maker_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(pe => pe.CheckerId)
            .HasColumnName("checker_id")
            .HasMaxLength(100);

        builder.Property(pe => pe.CorrectionDate)
            .HasColumnName("correction_date");

        builder.Property(pe => pe.CheckoutId)
            .HasColumnName("checkout")
            .HasMaxLength(150); // USERID.RANDOMSTRING

        builder.Property(pe => pe.ApprovedDate)
            .HasColumnName("checked_date");
            
        builder.Property(pe => pe.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(pe => pe.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes for frequently queried fields
        builder.HasIndex(pe => pe.DepartmentId);
        builder.HasIndex(pe => pe.ValueDate);
        builder.HasIndex(pe => pe.Status);
        builder.HasIndex(pe => pe.MakerId);
        builder.HasIndex(pe => pe.CheckoutId);
    }
}