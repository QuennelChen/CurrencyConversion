namespace CurrencyConversion.Api.Options;

public class AuthOptions
{
    public const string SectionName = "Authentication";
    public string Scheme { get; set; } = "ApiKey"; // or Jwt
    public string? ApiKey { get; set; }
    public string? JwtSecret { get; set; }
}
