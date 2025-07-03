using CurrencyConversion.Api.Models;

namespace CurrencyConversion.Api.Repositories;

public interface IExchangeRateLogRepository
{
    Task<ExchangeRateLog?> GetLatestRateAsync(int baseCurrencyId, int targetCurrencyId);
    Task<List<ExchangeRateLog>> GetLatestRatesForBaseCurrencyAsync(int baseCurrencyId);
    Task<bool> HasAnyDataAsync();
    Task<int> GetRecordCountAsync();
    Task<DateTime?> GetLastSyncTimeAsync();
    Task AddAsync(ExchangeRateLog log);
    Task SaveChangesAsync();
}
