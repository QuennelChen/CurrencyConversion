using CurrencyConversion.Api.Models;
using CurrencyConversion.Api.Repositories;
using CurrencyConversion.Api.Services;
using Moq;
using Xunit;

namespace CurrencyConversion.Tests;

public class ExchangeRateServiceTests
{
    [Fact]
    public async Task ConvertAsync_ReturnsConvertedAmount()
    {
        var currencyRepo = new Mock<ICurrencyRepository>();
        var logRepo = new Mock<IExchangeRateLogRepository>();

        currencyRepo.Setup(r => r.GetByCodeAsync("USD")).ReturnsAsync(new Currency { Id = 1, Code = "USD", Name = "US Dollar" });
        currencyRepo.Setup(r => r.GetByCodeAsync("TWD")).ReturnsAsync(new Currency { Id = 2, Code = "TWD", Name = "Taiwan Dollar" });
        logRepo.Setup(r => r.GetLatestRateAsync(1, 2)).ReturnsAsync(new ExchangeRateLog
        {
            BaseCurrencyId = 1,
            TargetCurrencyId = 2,
            Rate = 30m,
            RetrievedAt = DateTime.UtcNow
        });

        var service = new ExchangeRateService(currencyRepo.Object, logRepo.Object);

        var result = await service.ConvertAsync("USD", "TWD", 2);

        Assert.NotNull(result);
        Assert.Equal(60m, result?.converted);
    }
}
