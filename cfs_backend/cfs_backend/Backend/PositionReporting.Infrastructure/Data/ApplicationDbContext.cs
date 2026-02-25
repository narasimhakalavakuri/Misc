using Microsoft.EntityFrameworkCore;
using PositionReporting.Infrastructure.Data.Configurations;
using PositionReporting.Infrastructure.Data.Entities;
using System.Data.Common;
using Azure.Identity; // For DefaultAzureCredential
using Microsoft.Data.SqlClient; // For SqlCredential
using Microsoft.EntityFrameworkCore.Diagnostics; // For DbContextOptionsBuilder

namespace PositionReporting.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<PositionEntry> PositionEntries { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Currency> Currencies { get; set; }
    public DbSet<NostroAccount> NostroAccounts { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply Configurations
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
        modelBuilder.ApplyConfiguration(new CurrencyConfiguration());
        modelBuilder.ApplyConfiguration(new NostroAccountConfiguration());
        modelBuilder.ApplyConfiguration(new PositionEntryConfiguration());

        // Seed initial data if necessary
        modelBuilder.Entity<User>().HasData(
            new User { Id = Guid.Parse("C5E6A3B9-2E8B-4B2D-9D2E-7C1F8A6B4D3C"), UserId = "DOMAIN\\ADMIN", Email = "admin@yourorg.com", DepartmentId = "ADM", AccessMask = "1111111", IsActive = true, CreatedAt = DateTimeOffset.UtcNow },
            new User { Id = Guid.Parse("B8D2E7C1-F8A6-4B2D-9D2E-7C1F8A6B4D3D"), UserId = "DOMAIN\\USER1", Email = "user1@yourorg.com", DepartmentId = "FN", AccessMask = "1100010", IsActive = true, CreatedAt = DateTimeOffset.UtcNow }
        );
        modelBuilder.Entity<Department>().HasData(
            new Department { DepartmentCode = "FN", DepartmentDescription = "Finance Department", Status = "OPEN", CreatedAt = DateTimeOffset.UtcNow },
            new Department { DepartmentCode = "OPS", DepartmentDescription = "Operations Department", Status = "OPEN", CreatedAt = DateTimeOffset.UtcNow },
            new Department { DepartmentCode = "ADM", DepartmentDescription = "Administration", Status = "OPEN", CreatedAt = DateTimeOffset.UtcNow }
        );
        modelBuilder.Entity<Currency>().HasData(
            new Currency { CurrencyCode = "SGD", DecimalPrecision = 2, ExchangeRateTTS = 1.0f, CreatedAt = DateTimeOffset.UtcNow },
            new Currency { CurrencyCode = "USD", DecimalPrecision = 2, ExchangeRateTTS = 1.35f, CreatedAt = DateTimeOffset.UtcNow },
            new Currency { CurrencyCode = "JPY", DecimalPrecision = 0, ExchangeRateTTS = 0.009f, CreatedAt = DateTimeOffset.UtcNow }
        );
        modelBuilder.Entity<NostroAccount>().HasData(
            new NostroAccount { AccountNumber = "SGDBNK001", CurrencyCode = "SGD", CreatedAt = DateTimeOffset.UtcNow },
            new NostroAccount { AccountNumber = "USDBNK001", CurrencyCode = "USD", CreatedAt = DateTimeOffset.UtcNow }
        );
    }
}

// Interceptor for Azure AD authentication in local development
public class AzureAdAuthenticationDbConnectionInterceptor : DbConnectionInterceptor
{
    private readonly DefaultAzureCredential _credential;

    public AzureAdAuthenticationDbConnectionInterceptor(DefaultAzureCredential credential)
    {
        _credential = credential;
    }

    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        UpdateAccessToken(connection);
        return result;
    }

    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    {
        await UpdateAccessTokenAsync(connection);
        return result;
    }

    private void UpdateAccessToken(DbConnection connection)
    {
        if (connection is SqlConnection sqlConnection && string.IsNullOrEmpty(sqlConnection.AccessToken))
        {
            var token = _credential.GetToken(new Azure.Core.TokenRequestContext(new[] { "https://database.windows.net/.default" }));
            sqlConnection.AccessToken = token.Token;
        }
    }

    private async Task UpdateAccessTokenAsync(DbConnection connection)
    {
        if (connection is SqlConnection sqlConnection && string.IsNullOrEmpty(sqlConnection.AccessToken))
        {
            var token = await _credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { "https://database.windows.net/.default" }));
            sqlConnection.AccessToken = token.Token;
        }
    }
}