namespace CurrencyConversion.Api.Models;

public class Currency
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public ICollection<ExchangeRateLog> BaseRateLogs { get; set; } = new List<ExchangeRateLog>();
    public ICollection<ExchangeRateLog> TargetRateLogs { get; set; } = new List<ExchangeRateLog>();
}
