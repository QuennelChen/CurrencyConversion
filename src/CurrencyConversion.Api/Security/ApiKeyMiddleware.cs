using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using CurrencyConversion.Api.Options;

namespace CurrencyConversion.Api.Security;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthOptions _options;

    public ApiKeyMiddleware(RequestDelegate next, IOptions<AuthOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_options.Scheme != "ApiKey")
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var key) || key != _options.ApiKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid API Key");
            return;
        }
        await _next(context);
    }
}
