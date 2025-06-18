using CurrencyConversion.Api.Models;
using CurrencyConversion.Api.Repositories;

namespace CurrencyConversion.Api.Services;

public class ExchangeRateService : IExchangeRateService
{
    private readonly ICurrencyRepository _currencyRepo;
    private readonly IExchangeRateLogRepository _logRepo;

    public ExchangeRateService(ICurrencyRepository currencyRepo, IExchangeRateLogRepository logRepo)
    {
        _currencyRepo = currencyRepo;
        _logRepo = logRepo;
    }

    public async Task<IEnumerable<Currency>> GetCurrenciesAsync() => await _currencyRepo.GetAllAsync();

    public async Task<(ExchangeRateLog log, Currency from, Currency to)?> GetLatestRateAsync(string fromCode, string toCode)
    {
        var from = await _currencyRepo.GetByCodeAsync(fromCode);
        var to = await _currencyRepo.GetByCodeAsync(toCode);
        if (from == null || to == null) return null;
        var log = await _logRepo.GetLatestRateAsync(from.Id, to.Id);
        if (log == null) return null;
        return (log, from, to);
    }

    public async Task<(decimal converted, ExchangeRateLog log, Currency from, Currency to)?> ConvertAsync(string fromCode, string toCode, decimal amount)
    {
        var result = await GetLatestRateAsync(fromCode, toCode);
        if (result == null) return null;
        var (log, from, to) = result.Value;
        var converted = amount * log.Rate;
        return (converted, log, from, to);
    }
}
