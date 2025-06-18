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

builder.Services.AddControllers();

builder.Services.Configure<ExternalApiOptions>(builder.Configuration.GetSection(ExternalApiOptions.SectionName));
builder.Services.Configure<ScheduleOptions>(builder.Configuration.GetSection(ScheduleOptions.SectionName));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();
builder.Services.AddScoped<IExchangeRateLogRepository, ExchangeRateLogRepository>();
builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();

builder.Services.AddHttpClient<IExternalRateProvider, ExchangeRateHostProvider>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<ExternalApiOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl);
    if (!string.IsNullOrEmpty(opts.ApiKey))
        client.DefaultRequestHeaders.Add("apikey", opts.ApiKey);
});

builder.Services.AddHostedService<DailySyncService>();

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
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(authOptions.JwtSecret ?? "secret"))
            };
        });
}
else
{
    builder.Services.AddAuthentication("ApiKey");
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Currency API", Version = "v1" });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
