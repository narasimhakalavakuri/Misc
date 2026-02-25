using Microsoft.EntityFrameworkCore;
using PositionReporting.Infrastructure.Data;
using PositionReporting.Infrastructure.Data.Entities;
using PositionReporting.Infrastructure.Repositories.Interfaces;

namespace PositionReporting.Infrastructure.Repositories.Implementations;

public class CurrencyRepository : ICurrencyRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CurrencyRepository> _logger;

    public CurrencyRepository(ApplicationDbContext context, ILogger<CurrencyRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Currency>> GetAllAsync(int? limit, int? offset, string? sort, string? filter)
    {
        _logger.LogDebug("Retrieving all currencies with limit: {Limit}, offset: {Offset}, sort: {Sort}, filter: {Filter}", limit, offset, sort, filter);
        var query = _context.Currencies.AsNoTracking();

        // Apply filtering (simplified, real implementation would parse filter string)
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(c => c.CurrencyCode.Contains(filter) || c.DecimalPrecision.ToString().Contains(filter));
        }

        // Apply sorting (simplified, real implementation would parse sort string)
        if (!string.IsNullOrWhiteSpace(sort))
        {
            // Example: "currencyCode:asc"
            var parts = sort.Split(':');
            if (parts.Length == 2)
            {
                query = parts[0].ToLowerInvariant() switch
                {
                    "currencycode" => parts[1].ToLowerInvariant() == "desc" ? query.OrderByDescending(c => c.CurrencyCode) : query.OrderBy(c => c.CurrencyCode),
                    "decimalprecision" => parts[1].ToLowerInvariant() == "desc" ? query.OrderByDescending(c => c.DecimalPrecision) : query.OrderBy(c => c.DecimalPrecision),
                    _ => query // Default or throw error
                };
            }
        }
        else
        {
            query = query.OrderBy(c => c.CurrencyCode); // Default sort
        }

        // Apply pagination
        if (offset.HasValue)
        {
            query = query.Skip(offset.Value);
        }
        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<Currency?> GetByCodeAsync(string currencyCode)
    {
        _logger.LogDebug("Retrieving currency by code: {CurrencyCode}", currencyCode);
        return await _context.Currencies.AsNoTracking().FirstOrDefaultAsync(c => c.CurrencyCode == currencyCode);
    }

    public async Task<List<NostroAccount>> GetNostroAccountsByCurrencyAsync(string currencyCode)
    {
        _logger.LogDebug("Retrieving Nostro accounts for currency: {CurrencyCode}", currencyCode);
        // Assuming TBL_MISC.dataid1 = 'NOSTRO' and TBL_MISC.dataid2 = CurrencyCode
        return await _context.NostroAccounts
                             .AsNoTracking()
                             .Where(n => n.CurrencyCode == currencyCode && n.DataId1 == "NOSTRO")
                             .ToListAsync();
    }

    public Task<string> GetHomeCurrencyCodeAsync()
    {
        // This would typically come from a system settings table or configuration
        // For now, hardcode or retrieve from a simple lookup table
        return Task.FromResult("SGD"); // Example
    }

    public Task<bool> IsNostroAccount(string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber)) return Task.FromResult(false);
        // Check if an account number exists as a Nostro account in TBL_MISC
        return _context.NostroAccounts.AnyAsync(n => n.AccountNumber == accountNumber);
    }

    public async Task<CustomerAccount?> GetCustomerAccountAsync(string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber)) return null;
        // This would query a 'custfile' table as in Delphi
        // For demonstration, returning a dummy account if it matches a pattern
        if (accountNumber == "1234567890")
        {
            return new CustomerAccount { AccountNumber = accountNumber, AccountName = "ABC Bank" };
        }
        if (accountNumber == "0987654321")
        {
            return new CustomerAccount { AccountNumber = accountNumber, AccountName = "XYZ Corp" };
        }
        return null;
    }
}

// Dummy DTO for customer account details
public class CustomerAccount
{
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
}