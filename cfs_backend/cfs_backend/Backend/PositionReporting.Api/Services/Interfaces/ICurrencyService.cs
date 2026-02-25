using Kiota.ApiClient.Models;
using PositionReporting.Api.Controllers; // For CurrencyListRequest

namespace PositionReporting.Api.Services.Interfaces;

public interface ICurrencyService
{
    Task<List<Currency>> ListCurrenciesAsync(Controllers.CurrenciesController.CurrencyListRequest request);
    Task<Currency?> GetCurrencyByCodeAsync(string currencyCode);
    Task<List<NostroAccount>> ListNostroAccountsByCurrencyAsync(string currencyCode);
}