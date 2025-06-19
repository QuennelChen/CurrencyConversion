using Microsoft.Extensions.Diagnostics.HealthChecks;
using CurrencyConversion.Api.Services;

namespace CurrencyConversion.Api.Services;

/// <summary>
/// 外部 API 健康檢查
/// </summary>
public class ExternalApiHealthCheck : IHealthCheck
{
    private readonly IExternalRateProvider _rateProvider;
    private readonly ILogger<ExternalApiHealthCheck> _logger;

    public ExternalApiHealthCheck(IExternalRateProvider rateProvider, ILogger<ExternalApiHealthCheck> logger)
    {
        _rateProvider = rateProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("正在檢查外部匯率 API 健康狀態");
            
            // 嘗試取得 USD 的匯率資料來測試 API 連線
            var rates = await _rateProvider.GetRatesAsync("USD");
            
            if (rates.Count > 0)
            {
                _logger.LogDebug("外部 API 健康檢查通過，取得 {count} 個匯率", rates.Count);
                return HealthCheckResult.Healthy($"外部 API 正常，取得 {rates.Count} 個匯率");
            }
            else
            {
                _logger.LogWarning("外部 API 回應但無匯率資料");
                return HealthCheckResult.Degraded("外部 API 可連線但無資料");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "外部 API 健康檢查失敗");
            return HealthCheckResult.Unhealthy("外部 API 無法連線", ex);
        }
    }
}
