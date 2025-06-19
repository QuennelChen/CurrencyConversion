using CurrencyConversion.Api.Services;
using CurrencyConversion.Api.Repositories;
using CurrencyConversion.Api.HostedServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConversion.Api.Controllers;

/// <summary>
/// 匯率控制器 - 提供匯率查詢和貨幣轉換的 API 端點
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class ExchangeRateController : ControllerBase
{

    private readonly IExchangeRateService _service;
    private readonly IExternalRateProvider _externalRateProvider;

    /// <summary>
    /// 初始化匯率控制器
    /// </summary>
    /// <param name="service">匯率服務</param>
    /// <param name="externalRateProvider">外部匯率提供者</param>
    public ExchangeRateController(IExchangeRateService service, IExternalRateProvider externalRateProvider)
    {
        _service = service;
        _externalRateProvider = externalRateProvider;
    }    /// <summary>
    /// 取得最新匯率（從DB取得最新資料，如果資料過舊則觸發同步）
    /// </summary>
    /// <param name="baseCurrency">基準貨幣</param>
    /// <returns>所有對應匯率</returns>
    [HttpGet("rates")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLatestRates([FromQuery] string baseCurrency, [FromServices] ICurrencyRepository currencyRepo, [FromServices] IExchangeRateLogRepository logRepo)
    {
        if (string.IsNullOrWhiteSpace(baseCurrency)) return BadRequest("請提供 baseCurrency 參數");
        
        var currencies = await currencyRepo.GetAllAsync();
        var baseCur = currencies.FirstOrDefault(c => c.Code == baseCurrency);
        if (baseCur == null) return BadRequest("無效的 baseCurrency");
        
        var result = new Dictionary<string, object>();
        var rates = new Dictionary<string, decimal>();
        DateTime? lastUpdate = null;
        
        foreach (var target in currencies)
        {
            if (target.Code == baseCurrency) continue;
            var log = await logRepo.GetLatestRateAsync(baseCur.Id, target.Id);
            if (log != null)
            {
                rates[target.Code] = log.Rate;
                if (lastUpdate == null || log.RetrievedAt > lastUpdate)
                    lastUpdate = log.RetrievedAt;
            }
        }
        
        result["baseCurrency"] = baseCurrency;
        result["rates"] = rates;
        result["lastUpdate"] = lastUpdate;
        result["dataAge"] = lastUpdate.HasValue ? (TimeSpan?)(DateTime.UtcNow - lastUpdate.Value) : null;
        
        return Ok(result);
    }

    /// <summary>
    /// 取得兩種貨幣之間的最新匯率
    /// </summary>
    /// <param name="from">來源貨幣代碼</param>
    /// <param name="to">目標貨幣代碼</param>
    /// <returns>匯率資訊</returns>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string from, [FromQuery] string to)
    {
        var result = await _service.GetLatestRateAsync(from, to);
        if (result == null) return BadRequest("無效的貨幣代碼");
        var (log, fromCur, toCur) = result.Value;
        return Ok(new
        {
            from = fromCur.Code,
            to = toCur.Code,
            rate = log.Rate,
            timestamp = log.RetrievedAt
        });
    }

    /// <summary>
    /// 貨幣轉換請求模型
    /// </summary>
    /// <param name="From">來源貨幣代碼</param>
    /// <param name="To">目標貨幣代碼</param>
    /// <param name="Amount">轉換金額</param>
    public record ConvertRequest(string From, string To, decimal Amount);

    /// <summary>
    /// 執行貨幣轉換
    /// </summary>
    /// <param name="request">轉換請求</param>
    /// <returns>轉換結果</returns>
    [HttpPost("convert")]
    public async Task<IActionResult> Convert([FromBody] ConvertRequest request)
    {
        var result = await _service.ConvertAsync(request.From, request.To, request.Amount);
        if (result == null) return BadRequest("無效的貨幣代碼");
        var (converted, log, fromCur, toCur) = result.Value;
        return Ok(new
        {
            from = fromCur.Code,
            to = toCur.Code,
            amount = request.Amount,
            convertedAmount = converted,
            rate = log.Rate,
            timestamp = log.RetrievedAt
        });
    }    /// <summary>
    /// 立即同步匯率
    /// </summary>
    /// <returns>同步結果</returns>
    [HttpPost("sync-now")]
    [AllowAnonymous]
    public async Task<IActionResult> SyncNow([FromServices] IExternalRateProvider provider, [FromServices] ICurrencyRepository currencyRepo, [FromServices] IExchangeRateLogRepository logRepo, [FromServices] ILogger<DailySyncService> logger)
    {
        try
        {
            var syncStartTime = DateTime.UtcNow;
            var successCount = 0;
            var totalPairs = 0;
            var errors = new List<string>();

            logger.LogInformation("開始手動同步匯率");
            
            var currencies = await currencyRepo.GetAllAsync();
            foreach (var baseCurrency in currencies)
            {
                try
                {
                    var rates = await provider.GetRatesAsync(baseCurrency.Code);
                    foreach (var target in currencies)
                    {
                        if (baseCurrency.Code == target.Code) continue;
                        
                        totalPairs++;
                        if (rates.TryGetValue(target.Code, out var rate))
                        {
                            var log = new Models.ExchangeRateLog
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
                            errors.Add($"無法取得 {baseCurrency.Code} 到 {target.Code} 的匯率");
                        }
                    }
                }
                catch (Exception ex)
                {
                    var errorMsg = $"同步基準貨幣 {baseCurrency.Code} 時發生錯誤: {ex.Message}";
                    errors.Add(errorMsg);
                    logger.LogError(ex, errorMsg);
                }
            }
            
            await logRepo.SaveChangesAsync();
            var duration = DateTime.UtcNow - syncStartTime;
            var logMsg = $"手動同步匯率完成。成功: {successCount}/{totalPairs} 貨幣對，耗時: {duration.TotalSeconds:F2} 秒";
            logger.LogInformation(logMsg);
            
            return Ok(new 
            { 
                success = true, 
                message = "同步完成", 
                details = new 
                {
                    syncTime = syncStartTime,
                    duration = duration.TotalSeconds,
                    successCount,
                    totalPairs,
                    successRate = totalPairs > 0 ? (double)successCount / totalPairs * 100 : 0,
                    errors = errors.Take(10) // 只顯示前 10 個錯誤
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "手動同步匯率時發生嚴重錯誤");
            return StatusCode(500, new { success = false, message = $"同步失敗: {ex.Message}" });
        }
    }

    /// <summary>
    /// 取得系統同步狀態
    /// </summary>
    /// <returns>系統狀態資訊</returns>
    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _service.GetSyncStatusAsync();
        return Ok(status);
    }

    /// <summary>
    /// 取得所有支援的貨幣
    /// </summary>
    /// <returns>貨幣清單</returns>
    [HttpGet("currencies")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCurrencies()
    {
        var currencies = await _service.GetCurrenciesAsync();
        return Ok(currencies.Select(c => new { c.Code, c.Name }));
    }
}
