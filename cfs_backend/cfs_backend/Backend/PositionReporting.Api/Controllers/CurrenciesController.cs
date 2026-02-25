using Kiota.ApiClient.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PositionReporting.Api.Services.Interfaces;
using System.Net.Mime;

namespace PositionReporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CurrenciesController : ControllerBase
{
    private readonly ICurrencyService _currencyService;
    private readonly ILogger<CurrenciesController> _logger;

    public CurrenciesController(ICurrencyService currencyService, ILogger<CurrenciesController> logger)
    {
        _currencyService = currencyService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve a list of all supported currencies.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "CanQueryPositionEntries")] // Or a specific "CanViewCurrencies" policy
    [ProducesResponseType(typeof(List<Currency>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<List<Currency>>> ListCurrencies(
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        [FromQuery] string? sort,
        [FromQuery] string? filter,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var currencies = await _currencyService.ListCurrenciesAsync(new CurrencyListRequest
        {
            Limit = limit,
            Offset = offset,
            Sort = sort,
            Filter = filter
        });
        return Ok(currencies);
    }

    /// <summary>
    /// Retrieve details of a specific currency.
    /// </summary>
    [HttpGet("{currencyCode}")]
    [Authorize(Policy = "CanQueryPositionEntries")]
    [ProducesResponseType(typeof(Currency), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<Currency>> GetCurrencyByCode(
        [FromRoute] string currencyCode,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var currency = await _currencyService.GetCurrencyByCodeAsync(currencyCode);
        if (currency == null)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "Currency not found.", Details = $"Currency '{currencyCode}' does not exist." });
        }
        return Ok(currency);
    }

    /// <summary>
    /// Retrieve Nostro accounts for a specific currency.
    /// </summary>
    [HttpGet("{currencyCode}/nostro-accounts")]
    [Authorize(Policy = "CanQueryPositionEntries")]
    [ProducesResponseType(typeof(List<NostroAccount>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<List<NostroAccount>>> ListNostroAccountsByCurrency(
        [FromRoute] string currencyCode,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var accounts = await _currencyService.ListNostroAccountsByCurrencyAsync(currencyCode);
        if (accounts == null || !accounts.Any())
        {
            // Even if no accounts, 200 with empty list is typical for collections
            return Ok(new List<NostroAccount>());
        }
        return Ok(accounts);
    }

    // --- Helper DTOs for Kiota Integration ---
    public class CurrencyListRequest
    {
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public string? Sort { get; set; }
        public string? Filter { get; set; }
    }
}