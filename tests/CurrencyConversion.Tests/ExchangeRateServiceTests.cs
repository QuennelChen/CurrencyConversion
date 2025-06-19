using System;
using System.Threading.Tasks;
using CurrencyConversion.Api.Models;
using CurrencyConversion.Api.Repositories;
using CurrencyConversion.Api.Services;
using Moq;
using Xunit;

namespace CurrencyConversion.Tests;

/// <summary>
/// 匯率服務測試類別
/// </summary>
public class ExchangeRateServiceTests
{
    /// <summary>
    /// 測試貨幣轉換功能是否正確返回轉換金額
    /// </summary>
    [Fact]
    public async Task ConvertAsync_ReturnsConvertedAmount()
    {
        // 安排 (Arrange) - 設定模擬物件
        var currencyRepo = new Mock<ICurrencyRepository>();
        var logRepo = new Mock<IExchangeRateLogRepository>();

        // 設定模擬的美元貨幣資料
        currencyRepo.Setup(r => r.GetByCodeAsync("USD")).ReturnsAsync(new Currency { Id = 1, Code = "USD", Name = "美元" });
        // 設定模擬的台幣貨幣資料
        currencyRepo.Setup(r => r.GetByCodeAsync("TWD")).ReturnsAsync(new Currency { Id = 2, Code = "TWD", Name = "新台幣" });
        // 設定模擬的匯率記錄（1 美元 = 30 台幣）
        logRepo.Setup(r => r.GetLatestRateAsync(1, 2)).ReturnsAsync(new ExchangeRateLog
        {
            BaseCurrencyId = 1,
            TargetCurrencyId = 2,
            Rate = 30m,
            RetrievedAt = DateTime.UtcNow
        });

        var service = new ExchangeRateService(currencyRepo.Object, logRepo.Object);

        // 執行 (Act) - 測試轉換 2 美元到台幣
        var result = await service.ConvertAsync("USD", "TWD", 2);

        // 驗證 (Assert) - 確認結果正確
        Assert.NotNull(result);
        Assert.Equal(60m, result?.converted); // 2 * 30 = 60
    }
}
