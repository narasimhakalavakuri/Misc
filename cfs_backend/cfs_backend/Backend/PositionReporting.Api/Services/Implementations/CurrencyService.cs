using Kiota.ApiClient.Models;
using PositionReporting.Api.Controllers; // For CurrencyListRequest
using PositionReporting.Api.Services.Interfaces;
using PositionReporting.Infrastructure.Data.Entities;
using PositionReporting.Infrastructure.Repositories.Interfaces;

namespace PositionReporting.Api.Services.Implementations;

public class CurrencyService : ICurrencyService
{
    private readonly ICurrencyRepository _currencyRepository;
    private readonly ILogger<CurrencyService> _logger;

    public CurrencyService(ICurrencyRepository currencyRepository, ILogger<CurrencyService> logger)
    {
        _currencyRepository = currencyRepository;
        _logger = logger;
    }

    public async Task<List<Currency>> ListCurrenciesAsync(CurrencyListRequest request)
    {
        _logger.LogInformation("Listing currencies with Limit: {Limit}, Offset: {Offset}, Sort: {Sort}, Filter: {Filter}",
            request.Limit, request.Offset, request.Sort, request.Filter);

        var entities = await _currencyRepository.GetAllAsync(
            request.Limit,
            request.Offset,
            request.Sort,
            request.Filter);

        // Map internal entities to Kiota DTOs
        return entities.Select(MapToKiotaCurrency).ToList();
    }

    public async Task<Currency?> GetCurrencyByCodeAsync(string currencyCode)
    {
        _logger.LogInformation("Retrieving currency with code: {CurrencyCode}", currencyCode);
        var entity = await _currencyRepository.GetByCodeAsync(currencyCode);
        return entity == null ? null : MapToKiotaCurrency(entity);
    }

    public async Task<List<NostroAccount>> ListNostroAccountsByCurrencyAsync(string currencyCode)
    {
        _logger.LogInformation("Listing Nostro accounts for currency: {CurrencyCode}", currencyCode);
        var entities = await _currencyRepository.GetNostroAccountsByCurrencyAsync(currencyCode);
        return entities.Select(MapToKiotaNostroAccount).ToList();
    }

    private Currency MapToKiotaCurrency(Infrastructure.Data.Entities.Currency entity)
    {
        return new Currency
        {
            CurrencyCode = entity.CurrencyCode,
            DecimalPrecision = entity.DecimalPrecision,
            ExchangeRateTts = entity.ExchangeRateTTS
        };
    }

    private NostroAccount MapToKiotaNostroAccount(Infrastructure.Data.Entities.NostroAccount entity)
    {
        return new NostroAccount
        {
            AccountNumber = entity.AccountNumber
        };
    }
}