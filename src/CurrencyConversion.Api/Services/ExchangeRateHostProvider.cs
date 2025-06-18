using System.Text.Json;

namespace CurrencyConversion.Api.Services;

public class ExchangeRateHostProvider : IExternalRateProvider
{
    private readonly HttpClient _client;
    public ExchangeRateHostProvider(HttpClient client)
    {
        _client = client;
    }

    public async Task<Dictionary<string, decimal>> GetRatesAsync(string baseCurrencyCode)
    {
        var response = await _client.GetAsync($"latest?base={baseCurrencyCode}");
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(stream);
        var rates = new Dictionary<string, decimal>();
        if (doc.RootElement.TryGetProperty("rates", out var ratesElem))
        {
            foreach (var rate in ratesElem.EnumerateObject())
            {
                rates[rate.Name] = rate.Value.GetDecimal();
            }
        }
        return rates;
    }
}
