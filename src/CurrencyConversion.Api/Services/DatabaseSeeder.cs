using CurrencyConversion.Api.Data;
using CurrencyConversion.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CurrencyConversion.Api.Services;

/// <summary>
/// 資料庫種子資料服務 - 初始化基本貨幣資料
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// 初始化基本貨幣資料
    /// </summary>
    /// <param name="context">資料庫內容</param>
    /// <param name="logger">日誌記錄器</param>
    public static async Task SeedCurrenciesAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Currencies.AnyAsync())
        {
            logger.LogInformation("貨幣資料已存在，跳過種子資料初始化");
            return;
        }

        logger.LogInformation("正在初始化基本貨幣資料");

        var currencies = new List<Currency>
        {
            new Currency { Code = "USD", Name = "美元" },
            new Currency { Code = "EUR", Name = "歐元" },
            new Currency { Code = "JPY", Name = "日圓" },
            new Currency { Code = "GBP", Name = "英鎊" },
            new Currency { Code = "TWD", Name = "新台幣" },
            new Currency { Code = "CNY", Name = "人民幣" },
            new Currency { Code = "HKD", Name = "港幣" },
            new Currency { Code = "KRW", Name = "韓圓" },
            new Currency { Code = "SGD", Name = "新加坡幣" },
            new Currency { Code = "AUD", Name = "澳幣" },
            new Currency { Code = "CAD", Name = "加拿大幣" },
            new Currency { Code = "CHF", Name = "瑞士法郎" }
        };

        await context.Currencies.AddRangeAsync(currencies);
        await context.SaveChangesAsync();

        logger.LogInformation("成功初始化 {count} 種貨幣資料", currencies.Count);
    }
}
