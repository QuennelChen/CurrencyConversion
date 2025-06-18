# Currency Conversion API

This project provides a simple ASP.NET Core Web API for recording and querying currency exchange rates.

## Prerequisites

- .NET 7 SDK
- Microsoft SQL Server

## Setup

1. Restore packages and create database migrations:

```bash
dotnet restore
cd src/CurrencyConversion.Api
dotnet ef migrations add InitialCreate
```

2. Apply migrations and run the API:

```bash
dotnet ef database update
dotnet run
```

The daily synchronization job runs at the time specified in `appsettings.json` under `Schedule:DailyTime`.

## Configuration

`appsettings.json` contains placeholders for database connection string, external API key and authentication options. Replace them with your actual values before running the application.

## Testing

Example unit tests reside in `tests/CurrencyConversion.Tests`. Execute them with:

```bash
dotnet test
```
