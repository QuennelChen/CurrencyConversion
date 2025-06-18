using CurrencyConversion.Api.Models;

namespace CurrencyConversion.Api.Repositories;

public interface IExchangeRateLogRepository
{
    Task<ExchangeRateLog?> GetLatestRateAsync(int baseCurrencyId, int targetCurrencyId);
    Task AddAsync(ExchangeRateLog log);
    Task SaveChangesAsync();
}
