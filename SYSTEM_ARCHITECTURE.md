# Currency Conversion API 系統架構總結

## 系統概述

這是一個完整的貨幣轉換 API 系統，每天自動同步外部匯率資料到資料庫，並提供匯率查詢和轉換服務。

## 核心特性

### 1. 每日自動同步
- **服務**: `DailySyncService` (BackgroundService)
- **時間**: 每天早上 02:00 (可在 appsettings.json 中配置)
- **功能**: 
  - 自動從外部 API 取得最新匯率
  - 將資料儲存到資料庫
  - 支援錯誤重試機制
  - 詳細的日誌記錄

### 2. 資料來源
- **提供者**: `RterRateProvider`
- **API**: tw.rter.info
- **特性**:
  - HTTP 超時設定 (30秒)
  - 最多重試 3 次
  - 遞增延遲重試機制
  - 全面的錯誤處理

### 3. 支援的貨幣
系統支援 12 種主要貨幣：
- USD (美元)
- EUR (歐元) 
- JPY (日圓)
- GBP (英鎊)
- TWD (新台幣)
- CNY (人民幣)
- HKD (港幣)
- KRW (韓圓)
- SGD (新加坡幣)
- AUD (澳幣)
- CAD (加拿大幣)
- CHF (瑞士法郎)

## API 端點

### 貨幣管理
- `GET /api/ExchangeRate/currencies` - 取得支援的貨幣清單

### 匯率查詢
- `GET /api/ExchangeRate/rates?baseCurrency={code}` - 取得特定基準貨幣的所有匯率
- `GET /api/ExchangeRate?from={from}&to={to}` - 取得兩種貨幣間的匯率

### 貨幣轉換
- `POST /api/ExchangeRate/convert` - 執行貨幣轉換計算

### 系統管理
- `GET /api/ExchangeRate/status` - 取得系統同步狀態
- `POST /api/ExchangeRate/sync-now` - 立即手動同步匯率

### 健康檢查
- `GET /health` - 系統健康狀態檢查

## 資料庫架構

### 主要表格
1. **Currencies** - 貨幣基本資料
   - Id (主鍵)
   - Code (貨幣代碼)
   - Name (貨幣名稱)

2. **ExchangeRateLogs** - 匯率歷史記錄
   - Id (主鍵)
   - BaseCurrencyId (基準貨幣)
   - TargetCurrencyId (目標貨幣)
   - Rate (匯率)
   - RetrievedAt (取得時間)

## 系統特性

### 啟動流程
1. 檢查資料庫並執行遷移
2. 初始化基本貨幣資料（如果不存在）
3. 啟動每日同步服務
4. 如果資料庫無匯率資料，立即執行初始同步

### 錯誤處理與監控
- 完整的日誌記錄系統
- 重試機制與失敗處理
- 健康檢查端點
- 系統狀態監控

### 認證與安全
- API Key 認證中介軟體
- 支援 JWT 認證（可選）
- Swagger UI 整合

## 配置設定

### appsettings.json 主要設定
```json
{
  "ConnectionStrings": {
    "Default": "資料庫連線字串"
  },
  "Schedule": {
    "DailyTime": "02:00"  // 每日同步時間
  },
  "Authentication": {
    "Scheme": "ApiKey",   // 認證方式
    "ApiKey": "您的API金鑰",
    "JwtSecret": "JWT密鑰"
  }
}
```

## 部署與運行

### 開發環境
```bash
cd src/CurrencyConversion.Api
dotnet run
```

### 生產環境
```bash
dotnet publish -c Release
# 設定適當的環境變數和配置
# 部署到目標伺服器
```

## 監控與維護

### 日誌級別
- **Information**: 正常操作記錄
- **Warning**: 部分匯率取得失敗
- **Error**: 嚴重錯誤需要處理

### 關鍵監控指標
- 同步成功率
- 資料完整性百分比
- API 回應時間
- 外部 API 健康狀態

## 未來改進建議

1. **多資料來源支援**: 整合多個匯率提供者以提高可靠性
2. **快取機制**: 實施 Redis 快取以提升查詢效能
3. **歷史資料查詢**: 提供特定日期的匯率查詢
4. **告警系統**: 當同步失敗時發送通知
5. **效能優化**: 批次處理和異步操作優化

## 系統架構圖

```
External API (tw.rter.info)
         ↓
    RterRateProvider
         ↓
    DailySyncService ----→ Database
         ↓                    ↑
    ExchangeRateService ------┘
         ↓
    ExchangeRateController
         ↓
       API 端點
```

此系統提供了一個完整、可靠的貨幣轉換服務，具備自動同步、錯誤處理、監控和管理功能。
