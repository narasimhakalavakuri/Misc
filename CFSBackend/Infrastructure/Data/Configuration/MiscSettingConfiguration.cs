using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectName.Infrastructure.Data.Entities;

namespace ProjectName.Infrastructure.Data.Configuration
{
    public class MiscSettingConfiguration : IEntityTypeConfiguration<MiscSetting>
    {
        public void Configure(EntityTypeBuilder<MiscSetting> builder)
        {
            builder.ToTable("misc_settings");

            builder.HasKey(ms => ms.Id);
            builder.Property(ms => ms.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()")
                .IsRequired();

            builder.Property(ms => ms.DataId1)
                .HasColumnName("dataid1")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(ms => ms.DataId2)
                .HasColumnName("dataid2")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(ms => ms.Data01)
                .HasColumnName("data01")
                .HasMaxLength(255);

            builder.Property(ms => ms.Data02)
                .HasColumnName("data02")
                .HasMaxLength(255);

            // Add other DataXX properties as needed based on Delphi usage
        }
    }
}