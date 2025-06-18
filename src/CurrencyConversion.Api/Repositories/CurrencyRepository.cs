using CurrencyConversion.Api.Data;
using CurrencyConversion.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CurrencyConversion.Api.Repositories;

public class CurrencyRepository : ICurrencyRepository
{
    private readonly AppDbContext _context;
    public CurrencyRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Currency>> GetAllAsync() => await _context.Currencies.AsNoTracking().ToListAsync();

    public async Task<Currency?> GetByCodeAsync(string code) =>
        await _context.Currencies.FirstOrDefaultAsync(c => c.Code == code);
}
