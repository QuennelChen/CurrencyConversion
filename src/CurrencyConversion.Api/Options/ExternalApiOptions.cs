namespace CurrencyConversion.Api.Options;

public class ExternalApiOptions
{
    public const string SectionName = "ExternalApi";
    public string BaseUrl { get; set; } = "https://api.exchangerate.host/";
    public string? ApiKey { get; set; }
}
