using Microsoft.Extensions.Options;
using CurrencyConversion.Api.Data;
using CurrencyConversion.Api.HostedServices;
using CurrencyConversion.Api.Options;
using CurrencyConversion.Api.Repositories;
using CurrencyConversion.Api.Security;
using CurrencyConversion.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 添加控制器服務
builder.Services.AddControllers();

// 添加健康檢查
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("Default")!, name: "database")
    .AddCheck<ExternalApiHealthCheck>("external-api");

// 設定組態選項
builder.Services.Configure<ScheduleOptions>(builder.Configuration.GetSection(ScheduleOptions.SectionName));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));

// 設定資料庫內容
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// 註冊存放庫和服務
builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();
builder.Services.AddScoped<IExchangeRateLogRepository, ExchangeRateLogRepository>();
builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();

// 設定 HTTP 客戶端用於外部匯率提供者 (tw.rter.info)
builder.Services.AddHttpClient<IExternalRateProvider, RterRateProvider>();

// 註冊每日同步背景服務
builder.Services.AddHostedService<DailySyncService>();

// 設定身份驗證
var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>();
if (authOptions?.Scheme == "Jwt")
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(authOptions.JwtSecret ?? "secret"))            };
        });
}
else
{
    // 使用 API 金鑰身份驗證
    builder.Services.AddAuthentication("ApiKey");
}

// 設定 API 文件
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "貨幣 API", Version = "v1" });
});

var app = builder.Build();

// 確保資料庫已遷移並初始化種子資料
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("正在檢查資料庫狀態");
    db.Database.Migrate();
    
    // 初始化基本貨幣資料
    await DatabaseSeeder.SeedCurrenciesAsync(db, logger);
}

// 設定中介軟體管道
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// 健康檢查端點
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();
