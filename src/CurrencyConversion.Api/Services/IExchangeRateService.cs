using CurrencyConversion.Api.Models;

namespace CurrencyConversion.Api.Services;

public interface IExchangeRateService
{
    Task<IEnumerable<Currency>> GetCurrenciesAsync();
    Task<(ExchangeRateLog log, Currency from, Currency to)?> GetLatestRateAsync(string fromCode, string toCode);
    Task<(decimal converted, ExchangeRateLog log, Currency from, Currency to)?> ConvertAsync(string fromCode, string toCode, decimal amount);
}
