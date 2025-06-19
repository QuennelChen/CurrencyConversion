using CurrencyConversion.Api.Models;
using CurrencyConversion.Api.Options;
using CurrencyConversion.Api.Repositories;
using CurrencyConversion.Api.Services;
using Microsoft.Extensions.Options;

namespace CurrencyConversion.Api.HostedServices;

/// <summary>
/// 每日同步服務 - 定期從外部 API 同步匯率資料
/// </summary>
public class DailySyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ScheduleOptions> _scheduleOptions;
    private readonly ILogger<DailySyncService> _logger;
    private readonly IExternalRateProvider _provider;

    /// <summary>
    /// 初始化每日同步服務
    /// </summary>
    /// <param name="scopeFactory">服務範圍工廠</param>
    /// <param name="scheduleOptions">排程選項</param>
    /// <param name="logger">日誌記錄器</param>
    /// <param name="provider">外部匯率提供者</param>
    public DailySyncService(IServiceScopeFactory scopeFactory,
        IOptions<ScheduleOptions> scheduleOptions,
        ILogger<DailySyncService> logger,
        IExternalRateProvider provider)
    {
        _scopeFactory = scopeFactory;
        _scheduleOptions = scheduleOptions;
        _logger = logger;
        _provider = provider;
    }    /// <summary>
    /// 執行背景服務的主要邏輯
    /// </summary>
    /// <param name="stoppingToken">停止權杖</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("每日同步服務啟動");
        
        // 啟動時立即執行一次同步（如果DB為空的話）
        await CheckAndInitialSync(stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelay();
            _logger.LogInformation("下次同步時間: {nextSync}", DateTime.Now.Add(delay));
            
            try
            {
                await Task.Delay(delay, stoppingToken);
                await SyncRatesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("同步服務已取消");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "執行同步時發生未預期的錯誤");
                // 發生錯誤時等待 1 小時後重試
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    /// <summary>
    /// 檢查並執行初始同步
    /// </summary>
    /// <param name="stoppingToken">停止權杖</param>
    private async Task CheckAndInitialSync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var logRepo = scope.ServiceProvider.GetRequiredService<IExchangeRateLogRepository>();
        
        var hasData = await logRepo.HasAnyDataAsync();
        if (!hasData)
        {
            _logger.LogInformation("檢測到資料庫無匯率資料，執行初始同步");
            await SyncRatesAsync(stoppingToken);
        }
    }

    /// <summary>
    /// 計算到下次執行的延遲時間
    /// </summary>
    /// <returns>延遲時間</returns>
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
        // 如果無法解析時間，預設 24 小時後執行
        return TimeSpan.FromHours(24);
    }    /// <summary>
    /// 同步匯率資料
    /// </summary>
    /// <param name="ct">取消權杖</param>
    private async Task SyncRatesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var currencyRepo = scope.ServiceProvider.GetRequiredService<ICurrencyRepository>();
        var logRepo = scope.ServiceProvider.GetRequiredService<IExchangeRateLogRepository>();
        
        try
        {
            _logger.LogInformation("開始同步匯率資料");
            var syncStartTime = DateTime.UtcNow;
            var successCount = 0;
            var totalPairs = 0;
            
            var currencies = await currencyRepo.GetAllAsync();
            _logger.LogInformation("找到 {count} 種貨幣", currencies.Count());
            
            // 為每種基礎貨幣取得對所有其他貨幣的匯率
            foreach (var baseCurrency in currencies)
            {
                try
                {
                    _logger.LogDebug("正在同步基準貨幣: {currency}", baseCurrency.Code);
                    var rates = await _provider.GetRatesAsync(baseCurrency.Code);
                    
                    foreach (var target in currencies)
                    {
                        if (baseCurrency.Code == target.Code) continue;
                        
                        totalPairs++;
                        if (rates.TryGetValue(target.Code, out var rate))
                        {
                            var log = new ExchangeRateLog
                            {
                                BaseCurrencyId = baseCurrency.Id,
                                TargetCurrencyId = target.Id,
                                Rate = rate,
                                RetrievedAt = syncStartTime
                            };
                            await logRepo.AddAsync(log);
                            successCount++;
                        }
                        else
                        {
                            _logger.LogWarning("無法取得 {from} 到 {to} 的匯率", baseCurrency.Code, target.Code);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "同步基準貨幣 {currency} 時發生錯誤", baseCurrency.Code);
                }
            }
            
            await logRepo.SaveChangesAsync();
            var duration = DateTime.UtcNow - syncStartTime;
            _logger.LogInformation("匯率同步完成。成功: {success}/{total} 貨幣對，耗時: {duration}ms", 
                successCount, totalPairs, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步匯率時發生嚴重錯誤");
            throw; // 重新拋出異常以便上層處理
        }
    }
}
