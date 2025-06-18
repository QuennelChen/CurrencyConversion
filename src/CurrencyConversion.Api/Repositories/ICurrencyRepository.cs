using CurrencyConversion.Api.Models;

namespace CurrencyConversion.Api.Repositories;

public interface ICurrencyRepository
{
    Task<IEnumerable<Currency>> GetAllAsync();
    Task<Currency?> GetByCodeAsync(string code);
}
