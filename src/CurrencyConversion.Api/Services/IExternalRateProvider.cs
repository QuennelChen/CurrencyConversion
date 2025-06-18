namespace CurrencyConversion.Api.Services;

public interface IExternalRateProvider
{
    Task<Dictionary<string, decimal>> GetRatesAsync(string baseCurrencyCode);
}
