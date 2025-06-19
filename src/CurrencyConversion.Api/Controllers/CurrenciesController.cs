using CurrencyConversion.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConversion.Api.Controllers;

/// <summary>
/// 貨幣控制器 - 提供貨幣相關的 API 端點
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CurrenciesController : ControllerBase
{
    private readonly IExchangeRateService _service;
    
    /// <summary>
    /// 初始化貨幣控制器
    /// </summary>
    /// <param name="service">匯率服務</param>
    public CurrenciesController(IExchangeRateService service) => _service = service;

    /// <summary>
    /// 取得所有支援的貨幣清單
    /// </summary>
    /// <returns>貨幣代碼和名稱的清單</returns>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var currencies = await _service.GetCurrenciesAsync();
        return Ok(currencies.Select(c => new { c.Code, c.Name }));
    }
}
