using CurrencyConversion.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConversion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CurrenciesController : ControllerBase
{
    private readonly IExchangeRateService _service;
    public CurrenciesController(IExchangeRateService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var currencies = await _service.GetCurrenciesAsync();
        return Ok(currencies.Select(c => new { c.Code, c.Name }));
    }
}
