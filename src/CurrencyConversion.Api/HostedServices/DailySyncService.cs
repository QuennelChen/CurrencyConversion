using CurrencyConversion.Api.Models;
using CurrencyConversion.Api.Options;
using CurrencyConversion.Api.Repositories;
using CurrencyConversion.Api.Services;
using Microsoft.Extensions.Options;

namespace CurrencyConversion.Api.HostedServices;

public class DailySyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ScheduleOptions> _scheduleOptions;
    private readonly ILogger<DailySyncService> _logger;
    private readonly IExternalRateProvider _provider;

    public DailySyncService(IServiceScopeFactory scopeFactory,
        IOptions<ScheduleOptions> scheduleOptions,
        ILogger<DailySyncService> logger,
        IExternalRateProvider provider)
    {
        _scopeFactory = scopeFactory;
        _scheduleOptions = scheduleOptions;
        _logger = logger;
        _provider = provider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelay();
            await Task.Delay(delay, stoppingToken);
            await SyncRatesAsync(stoppingToken);
        }
    }

    private TimeSpan GetDelay()
    {
        if (TimeSpan.TryParse(_scheduleOptions.Value.DailyTime, out var time))
        {
            var next = DateTime.Today.Add(time);
            if (next < DateTime.Now)
            {
                next = next.AddDays(1);
            }
            return next - DateTime.Now;
        }
        return TimeSpan.FromHours(24);
    }

    private async Task SyncRatesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var currencyRepo = scope.ServiceProvider.GetRequiredService<ICurrencyRepository>();
        var logRepo = scope.ServiceProvider.GetRequiredService<IExchangeRateLogRepository>();
        try
        {
            var currencies = await currencyRepo.GetAllAsync();
            foreach (var baseCurrency in currencies)
            {
                var rates = await _provider.GetRatesAsync(baseCurrency.Code);
                foreach (var target in currencies)
                {
                    if (baseCurrency.Code == target.Code || !rates.TryGetValue(target.Code, out var rate))
                        continue;
                    var log = new ExchangeRateLog
                    {
                        BaseCurrencyId = baseCurrency.Id,
                        TargetCurrencyId = target.Id,
                        Rate = rate,
                        RetrievedAt = DateTime.UtcNow
                    };
                    await logRepo.AddAsync(log);
                }
            }
            await logRepo.SaveChangesAsync();
            _logger.LogInformation("Exchange rates synced successfully at {time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing exchange rates");
        }
    }
}
