using CurrencyConversion.Api.Models;
using CurrencyConversion.Api.Repositories;
using System.Linq;

namespace CurrencyConversion.Api.Services;

/// <summary>
/// 匯率服務實作 - 處理匯率查詢和貨幣轉換邏輯
/// </summary>
public class ExchangeRateService : IExchangeRateService
{
    private readonly ICurrencyRepository _currencyRepo;
    private readonly IExchangeRateLogRepository _logRepo;

    /// <summary>
    /// 初始化匯率服務
    /// </summary>
    /// <param name="currencyRepo">貨幣存放庫</param>
    /// <param name="logRepo">匯率記錄存放庫</param>
    public ExchangeRateService(ICurrencyRepository currencyRepo, IExchangeRateLogRepository logRepo)
    {
        _currencyRepo = currencyRepo;
        _logRepo = logRepo;
    }

    /// <summary>
    /// 取得所有支援的貨幣
    /// </summary>
    /// <returns>貨幣清單</returns>
    public async Task<IEnumerable<Currency>> GetCurrenciesAsync() => await _currencyRepo.GetAllAsync();

    /// <summary>
    /// 取得兩種貨幣之間的最新匯率
    /// </summary>
    /// <param name="fromCode">來源貨幣代碼</param>
    /// <param name="toCode">目標貨幣代碼</param>
    /// <returns>匯率記錄和相關貨幣資訊</returns>
    public async Task<(ExchangeRateLog log, Currency from, Currency to)?> GetLatestRateAsync(string fromCode, string toCode)
    {
        var from = await _currencyRepo.GetByCodeAsync(fromCode);
        var to = await _currencyRepo.GetByCodeAsync(toCode);
        if (from == null || to == null) return null;
        var log = await _logRepo.GetLatestRateAsync(from.Id, to.Id);
        if (log == null) return null;
        return (log, from, to);
    }    /// <summary>
    /// 執行貨幣轉換計算
    /// </summary>
    /// <param name="fromCode">來源貨幣代碼</param>
    /// <param name="toCode">目標貨幣代碼</param>
    /// <param name="amount">轉換金額</param>
    /// <returns>轉換結果和相關資訊</returns>
    public async Task<(decimal converted, ExchangeRateLog log, Currency from, Currency to)?> ConvertAsync(string fromCode, string toCode, decimal amount)
    {
        var result = await GetLatestRateAsync(fromCode, toCode);
        if (result == null) return null;
        var (log, from, to) = result.Value;
        var converted = amount * log.Rate;
        return (converted, log, from, to);
    }

    /// <summary>
    /// 取得系統同步狀態
    /// </summary>
    /// <returns>同步狀態資訊</returns>
    public async Task<object> GetSyncStatusAsync()
    {
        var recordCount = await _logRepo.GetRecordCountAsync();
        var lastSync = await _logRepo.GetLastSyncTimeAsync();
        var currencies = await GetCurrenciesAsync();
        var currencyCount = currencies.Count();
        
        return new
        {
            totalRecords = recordCount,
            lastSyncTime = lastSync,
            supportedCurrencies = currencyCount,
            expectedRecords = currencyCount * (currencyCount - 1), // n*(n-1) 對於雙向匯率
            dataCompleteness = currencyCount > 1 ? (double)recordCount / (currencyCount * (currencyCount - 1)) * 100 : 0,
            dataAge = lastSync.HasValue ? (TimeSpan?)(DateTime.UtcNow - lastSync.Value) : null
        };
    }

    /// <summary>
    /// 取得特定基準貨幣的所有匯率
    /// </summary>
    /// <param name="baseCurrencyCode">基準貨幣代碼</param>
    /// <returns>匯率字典</returns>
    public async Task<Dictionary<string, decimal>?> GetRatesForBaseCurrencyAsync(string baseCurrencyCode)
    {
        var baseCurrency = await _currencyRepo.GetByCodeAsync(baseCurrencyCode);
        if (baseCurrency == null) return null;

        var currencies = await GetCurrenciesAsync();
        var currencyMap = currencies.ToDictionary(c => c.Id, c => c.Code);
        var logs = await _logRepo.GetLatestRatesForBaseCurrencyAsync(baseCurrency.Id);
        var rates = new Dictionary<string, decimal>();

        foreach (var log in logs)
        {
            if (currencyMap.TryGetValue(log.TargetCurrencyId, out var code))
            {
                rates[code] = log.Rate;
            }
        }

        return rates;
    }
}
