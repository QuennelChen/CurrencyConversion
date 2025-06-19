using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;

namespace CurrencyConversion.Api.Services;

/// <summary>
/// Rter 匯率提供者 - 從 tw.rter.info 取得匯率資料
/// </summary>
public class RterRateProvider : IExternalRateProvider
{
    private readonly HttpClient _client;
    private readonly ILogger<RterRateProvider> _logger;
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 1000;

    public RterRateProvider(HttpClient client, ILogger<RterRateProvider> logger)
    {
        _client = client;
        _logger = logger;
        _client.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<Dictionary<string, decimal>> GetRatesAsync(string baseCurrencyCode)
    {
        var rates = new Dictionary<string, decimal>();
        
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug("正在從 Rter API 取得 {currency} 的匯率資料 (嘗試 {attempt}/{max})", 
                    baseCurrencyCode, attempt, MaxRetries);
                
                var response = await _client.GetStringAsync("https://tw.rter.info/capi.php");
                
                if (string.IsNullOrWhiteSpace(response))
                {
                    _logger.LogWarning("Rter API 回應空內容");
                    continue;
                }

                var doc = JsonDocument.Parse(response);
                
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    var key = prop.Name;
                    if (key.StartsWith(baseCurrencyCode))
                    {
                        if (prop.Value.TryGetProperty("Exrate", out var exrate))
                        {
                            var toCode = key.Substring(baseCurrencyCode.Length);
                            if (!string.IsNullOrWhiteSpace(toCode) && exrate.TryGetDecimal(out var rate))
                            {
                                rates[toCode] = rate;
                            }
                        }
                    }
                }
                
                _logger.LogDebug("成功取得 {currency} 的 {count} 個匯率", baseCurrencyCode, rates.Count);
                return rates;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "HTTP 請求失敗 (嘗試 {attempt}/{max}): {message}", 
                    attempt, MaxRetries, ex.Message);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "請求逾時 (嘗試 {attempt}/{max})", attempt, MaxRetries);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON 解析失敗 (嘗試 {attempt}/{max}): {message}", 
                    attempt, MaxRetries, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得匯率時發生未預期錯誤 (嘗試 {attempt}/{max})", attempt, MaxRetries);
            }

            if (attempt < MaxRetries)
            {
                var delay = RetryDelayMs * attempt; // 遞增延遲
                _logger.LogInformation("等待 {delay}ms 後重試", delay);
                await Task.Delay(delay);
            }
        }
        
        _logger.LogError("經過 {attempts} 次嘗試後仍無法取得 {currency} 的匯率資料", MaxRetries, baseCurrencyCode);
        return rates; // 回傳空字典而非拋出異常
    }
}
