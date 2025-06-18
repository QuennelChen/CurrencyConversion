using CurrencyConversion.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConversion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExchangeRateController : ControllerBase
{
    private readonly IExchangeRateService _service;
    public ExchangeRateController(IExchangeRateService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string from, [FromQuery] string to)
    {
        var result = await _service.GetLatestRateAsync(from, to);
        if (result == null) return BadRequest("Invalid currency code");
        var (log, fromCur, toCur) = result.Value;
        return Ok(new
        {
            from = fromCur.Code,
            to = toCur.Code,
            rate = log.Rate,
            timestamp = log.RetrievedAt
        });
    }

    public record ConvertRequest(string From, string To, decimal Amount);

    [HttpPost("convert")]
    public async Task<IActionResult> Convert([FromBody] ConvertRequest request)
    {
        var result = await _service.ConvertAsync(request.From, request.To, request.Amount);
        if (result == null) return BadRequest("Invalid currency code");
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
    }
}
