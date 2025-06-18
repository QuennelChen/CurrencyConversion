using CurrencyConversion.Api.Data;
using CurrencyConversion.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CurrencyConversion.Api.Repositories;

public class ExchangeRateLogRepository : IExchangeRateLogRepository
{
    private readonly AppDbContext _context;
    public ExchangeRateLogRepository(AppDbContext context) => _context = context;

    public async Task<ExchangeRateLog?> GetLatestRateAsync(int baseCurrencyId, int targetCurrencyId)
    {
        return await _context.ExchangeRateLogs
            .Where(e => e.BaseCurrencyId == baseCurrencyId && e.TargetCurrencyId == targetCurrencyId)
            .OrderByDescending(e => e.RetrievedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(ExchangeRateLog log)
    {
        await _context.ExchangeRateLogs.AddAsync(log);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
