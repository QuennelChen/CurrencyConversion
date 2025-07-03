# 貨幣轉換 API

此專案提供一個簡單的 ASP.NET Core Web API，用於記錄和查詢匯率。

## 先決條件

- .NET 7 SDK
- Microsoft SQL Server

## 設定

1. 還原套件並建立資料庫遷移：

```bash
dotnet restore
cd src/CurrencyConversion.Api
dotnet ef migrations add InitialCreate
```

2. 套用遷移並執行 API：

```bash
dotnet ef database update
dotnet run
```

每日同步作業會在 `appsettings.json` 中 `Schedule:DailyTime` 設定的時間執行。

## 組態

`appsettings.json` 包含資料庫連接字串、外部 API 金鑰和身份驗證選項的佔位符。在執行應用程式之前，請將其替換為您的實際值。

## 測試

範例單元測試位於 `tests/CurrencyConversion.Tests` 中。執行測試：

```bash
dotnet test
```

## CLI 使用

本專案包含一個 `CurrencyConversion.Cli` 主控台程式，可透過 API 快速進行貨幣轉換。先啟動 API 後，在專案根目錄執行：

```bash
dotnet run --project src/CurrencyConversion.Cli -- --from USD --to TWD --amount 100
```

可選擇使用 `--api` 指定 API 位址（預設為 `http://localhost:5000`）。
