using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using CurrencyConversion.Api.Options;

namespace CurrencyConversion.Api.Security;

/// <summary>
/// API 金鑰中介軟體 - 驗證 API 金鑰身份驗證
/// </summary>
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthOptions _options;

    /// <summary>
    /// 初始化 API 金鑰中介軟體
    /// </summary>
    /// <param name="next">下一個中介軟體</param>
    /// <param name="options">身份驗證選項</param>
    public ApiKeyMiddleware(RequestDelegate next, IOptions<AuthOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    /// <summary>
    /// 處理 HTTP 請求並驗證 API 金鑰
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // 如果 endpoint 有 AllowAnonymous，直接通過
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>() != null)
        {
            await _next(context);
            return;
        }

        // 如果不是使用 API 金鑰驗證方案，則略過
        if (_options.Scheme != "ApiKey")
        {
            await _next(context);
            return;
        }

        // 檢查請求標頭中是否包含有效的 API 金鑰
        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var key) || key != _options.ApiKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("無效的 API 金鑰");
            return;
        }
        await _next(context);
    }
}
