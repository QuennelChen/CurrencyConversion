namespace CurrencyConversion.Api.Models;

public class ExchangeRateLog
{
    public int Id { get; set; }
    public int BaseCurrencyId { get; set; }
    public int TargetCurrencyId { get; set; }
    public decimal Rate { get; set; }
    public DateTime RetrievedAt { get; set; }

    public Currency? BaseCurrency { get; set; }
    public Currency? TargetCurrency { get; set; }
}
