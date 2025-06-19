using CurrencyConversion.Api.Models;

namespace CurrencyConversion.Api.Services;

/// <summary>
/// 匯率服務介面 - 定義匯率查詢和貨幣轉換的契約
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// 取得所有支援的貨幣
    /// </summary>
    /// <returns>貨幣清單</returns>
    Task<IEnumerable<Currency>> GetCurrenciesAsync();

    /// <summary>
    /// 取得兩種貨幣之間的最新匯率
    /// </summary>
    /// <param name="fromCode">來源貨幣代碼</param>
    /// <param name="toCode">目標貨幣代碼</param>
    /// <returns>匯率記錄和相關貨幣資訊</returns>
    Task<(ExchangeRateLog log, Currency from, Currency to)?> GetLatestRateAsync(string fromCode, string toCode);

    /// <summary>
    /// 執行貨幣轉換計算
    /// </summary>
    /// <param name="fromCode">來源貨幣代碼</param>
    /// <param name="toCode">目標貨幣代碼</param>
    /// <param name="amount">轉換金額</param>
    /// <returns>轉換結果和相關資訊</returns>
    Task<(decimal converted, ExchangeRateLog log, Currency from, Currency to)?> ConvertAsync(string fromCode, string toCode, decimal amount);

    /// <summary>
    /// 取得系統同步狀態
    /// </summary>
    /// <returns>同步狀態資訊</returns>
    Task<object> GetSyncStatusAsync();

    /// <summary>
    /// 取得特定基準貨幣的所有匯率
    /// </summary>
    /// <param name="baseCurrencyCode">基準貨幣代碼</param>
    /// <returns>匯率字典</returns>
    Task<Dictionary<string, decimal>?> GetRatesForBaseCurrencyAsync(string baseCurrencyCode);
}
