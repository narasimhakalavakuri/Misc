using PositionReporting.Infrastructure.Data.Entities;
using PositionReporting.Infrastructure.Repositories.Implementations; // For CustomerAccount

namespace PositionReporting.Infrastructure.Repositories.Interfaces;

public interface ICurrencyRepository
{
    Task<List<Currency>> GetAllAsync(int? limit, int? offset, string? sort, string? filter);
    Task<Currency?> GetByCodeAsync(string currencyCode);
    Task<List<NostroAccount>> GetNostroAccountsByCurrencyAsync(string currencyCode);
    Task<string> GetHomeCurrencyCodeAsync();
    Task<bool> IsNostroAccount(string? accountNumber);
    Task<CustomerAccount?> GetCustomerAccountAsync(string? accountNumber);
}