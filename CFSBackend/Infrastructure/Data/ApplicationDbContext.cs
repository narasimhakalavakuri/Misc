using Microsoft.EntityFrameworkCore;
using ProjectName.Infrastructure.Data.Configuration;
using ProjectName.Infrastructure.Data.Entities;

namespace ProjectName.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<PositionReport> PositionReports { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<MiscSetting> MiscSettings { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations for entities
            modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
            modelBuilder.ApplyConfiguration(new PositionReportConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new CustomerConfiguration());
            modelBuilder.ApplyConfiguration(new CurrencyConfiguration());
            modelBuilder.ApplyConfiguration(new MiscSettingConfiguration());
            modelBuilder.ApplyConfiguration(new AuditLogConfiguration());

            // Unique index for (DataId1, DataId2) for MiscSettings
            modelBuilder.Entity<MiscSetting>()
                .HasIndex(ms => new { ms.DataId1, ms.DataId2 })
                .IsUnique();

            // Seed initial data for MiscSettings for Nostro
            modelBuilder.Entity<MiscSetting>().HasData(
                new MiscSetting { Id = Guid.NewGuid(), DataId1 = "NOSTRO", DataId2 = "SGD", Data01 = "SGD_NOSTRO_ACC1", Data02 = "SG001" },
                new MiscSetting { Id = Guid.NewGuid(), DataId1 = "NOSTRO", DataId2 = "SGD", Data01 = "SGD_NOSTRO_ACC2", Data02 = "SG002" },
                new MiscSetting { Id = Guid.NewGuid(), DataId1 = "NOSTRO", DataId2 = "USD", Data01 = "USD_NOSTRO_ACC1", Data02 = "US001" },
                new MiscSetting { Id = Guid.NewGuid(), DataId1 = "NOSTRO", DataId2 = "JPY", Data01 = "JPY_NOSTRO_ACC1", Data02 = "JP001" },
                new MiscSetting { Id = Guid.NewGuid(), DataId1 = "CUSTOMER_DEFAULT", DataId2 = "DEFAULT", Data01 = "[NO CUSTOMER]", Data02 = "Default Customer Name" }
            );

            // Seed initial data for Customers for lookup
            modelBuilder.Entity<Customer>().HasData(
                new Customer { Id = Guid.NewGuid(), AcctNo = "100001", AbbrvName = "ACME", CustName1 = "Acme Corporation", HomeCurrency = "SGD" },
                new Customer { Id = Guid.NewGuid(), AcctNo = "100002", AbbrvName = "GLOBEX", CustName1 = "Globex Corporation", HomeCurrency = "USD" },
                new Customer { Id = Guid.NewGuid(), AcctNo = "100003", AbbrvName = "WAYNE", CustName1 = "Wayne Enterprises", HomeCurrency = "EUR" }
            );

        }
    }
}