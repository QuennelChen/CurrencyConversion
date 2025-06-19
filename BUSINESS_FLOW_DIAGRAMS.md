# 貨幣轉換系統流程圖與電商平台整合方案

## 1. 系統主要業務流程圖

### 1.1 每日自動同步流程

```mermaid
flowchart TD
    A[系統啟動] --> B{資料庫是否有<br/>匯率資料?}
    B -->|否| C[立即執行初始同步]
    B -->|是| D[啟動每日排程服務]
    C --> D
    D --> E[等待至設定時間<br/>每日 02:00]
    E --> F[從外部API取得匯率<br/>tw.rter.info]
    F --> G{API回應<br/>是否成功?}
    G -->|否| H[重試機制<br/>最多3次]
    H --> I{重試是否<br/>成功?}
    I -->|否| J[記錄錯誤日誌<br/>等待下次同步]
    I -->|是| K[解析匯率資料]
    G -->|是| K
    K --> L[儲存至資料庫<br/>ExchangeRateLogs表]
    L --> M[記錄成功日誌]
    M --> N[等待下一個週期]
    N --> E
    J --> N
    
    style A fill:#e1f5fe
    style F fill:#fff3e0
    style L fill:#e8f5e8
    style J fill:#ffebee
```

### 1.2 API 服務流程

```mermaid
flowchart TD
    A[客戶端請求] --> B[API認證<br/>ApiKey驗證]
    B --> C{認證是否<br/>通過?}
    C -->|否| D[回傳 401 未授權]
    C -->|是| E{請求類型}
    
    E -->|查詢支援貨幣| F[GET /api/ExchangeRate/currencies]
    E -->|查詢匯率| G[GET /api/ExchangeRate/rates]
    E -->|貨幣轉換| H[POST /api/ExchangeRate/convert]
    E -->|手動同步| I[POST /api/ExchangeRate/sync-now]
    
    F --> F1[從 Currencies 表查詢]
    F1 --> F2[回傳貨幣清單]
    
    G --> G1[從 ExchangeRateLogs 表<br/>查詢最新匯率]
    G1 --> G2{資料是否<br/>存在且新鮮?}
    G2 -->|否| G3[觸發即時同步]
    G2 -->|是| G4[回傳匯率資料]
    G3 --> G4
    
    H --> H1[驗證輸入參數]
    H1 --> H2[查詢對應匯率]
    H2 --> H3[執行轉換計算<br/>amount × rate]
    H3 --> H4[回傳轉換結果]
    
    I --> I1[立即執行同步作業]
    I1 --> I2[回傳同步狀態]
    
    style A fill:#e1f5fe
    style B fill:#fff3e0
    style D fill:#ffebee
    style F2 fill:#e8f5e8
    style G4 fill:#e8f5e8
    style H4 fill:#e8f5e8
    style I2 fill:#e8f5e8
```

## 2. 電商平台整合方案

### 2.1 整合架構圖

```mermaid
flowchart TB
    subgraph "電商平台"
        A[商品頁面] --> B[購物車]
        B --> C[結帳頁面]
        C --> D[訂單處理]
        D --> E[付款系統]
    end
    
    subgraph "貨幣轉換系統"
        F[Currency API] --> G[匯率查詢服務]
        F --> H[轉換計算服務]
        F --> I[支援貨幣管理]
    end
    
    subgraph "外部服務"
        J[tw.rter.info API] --> K[每日同步服務]
        K --> L[匯率資料庫]
    end
    
    A -->|顯示多幣別價格| F
    B -->|即時價格轉換| H
    C -->|結帳金額計算| H
    D -->|訂單金額記錄| H
    
    G --> L
    H --> L
    I --> L
    
    style A fill:#e3f2fd
    style B fill:#e3f2fd
    style C fill:#e3f2fd
    style F fill:#fff3e0
    style G fill:#fff3e0
    style H fill:#fff3e0
    style L fill:#e8f5e8
```

### 2.2 電商平台整合流程

```mermaid
flowchart TD
    A[用戶進入商品頁] --> B[檢測用戶地區/偏好貨幣]
    B --> C{是否為<br/>預設貨幣TWD?}
    C -->|是| D[直接顯示原價]
    C -->|否| E[調用匯率API<br/>GET /api/ExchangeRate/rates]
    E --> F[計算並顯示當地貨幣價格]
    F --> G[用戶瀏覽商品]
    
    G --> H[加入購物車]
    H --> I[購物車頁面<br/>顯示多幣別價格]
    I --> J[用戶進入結帳]
    J --> K[調用轉換API<br/>POST /api/ExchangeRate/convert]
    K --> L[確認最終付款金額]
    L --> M[生成訂單<br/>記錄原價與轉換金額]
    M --> N[進入付款流程]
    
    style A fill:#e1f5fe
    style E fill:#fff3e0
    style K fill:#fff3e0
    style M fill:#e8f5e8
    style N fill:#c8e6c9
```

### 2.3 實時價格更新流程

```mermaid
flowchart TD
    A[電商平台前端] --> B[建立WebSocket連線<br/>或使用Server-Sent Events]
    B --> C[監聽匯率更新事件]
    C --> D{匯率是否<br/>有更新?}
    D -->|否| C
    D -->|是| E[重新計算商品價格]
    E --> F[更新頁面顯示價格]
    F --> G[通知用戶價格變動]
    G --> C
    
    H[每日同步服務] --> I[匯率更新完成]
    I --> J[發送更新事件]
    J --> D
    
    style A fill:#e1f5fe
    style H fill:#fff3e0
    style F fill:#e8f5e8
    style G fill:#fff9c4
```

## 3. 技術整合細節

### 3.1 API 整合範例

#### 前端 JavaScript 整合
```javascript
// 取得支援的貨幣清單
async function getSupportedCurrencies() {
    const response = await fetch('/api/ExchangeRate/currencies', {
        headers: {
            'X-API-Key': 'your-api-key'
        }
    });
    return await response.json();
}

// 即時貨幣轉換
async function convertPrice(amount, fromCurrency, toCurrency) {
    const response = await fetch('/api/ExchangeRate/convert', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-API-Key': 'your-api-key'
        },
        body: JSON.stringify({
            fromCode: fromCurrency,
            toCode: toCurrency,
            amount: amount
        })
    });
    return await response.json();
}
```

#### 後端服務整合
```csharp
public class ProductService
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public async Task<ProductPriceDto> GetProductWithLocalPrice(
        int productId, string targetCurrency)
    {
        var product = await GetProduct(productId);
        
        // 調用貨幣轉換API
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", "your-api-key");
        
        var convertRequest = new
        {
            fromCode = "TWD",
            toCode = targetCurrency,
            amount = product.Price
        };
        
        var response = await client.PostAsJsonAsync(
            "http://currency-api/api/ExchangeRate/convert", 
            convertRequest);
            
        var convertResult = await response.Content
            .ReadFromJsonAsync<ConvertResultDto>();
            
        return new ProductPriceDto
        {
            ProductId = product.Id,
            OriginalPrice = product.Price,
            OriginalCurrency = "TWD",
            LocalPrice = convertResult.ConvertedAmount,
            LocalCurrency = targetCurrency,
            ExchangeRate = convertResult.Rate
        };
    }
}
```

### 3.2 快取策略

```mermaid
flowchart TD
    A[電商平台請求] --> B{Redis快取中<br/>是否有匯率?}
    B -->|有且未過期| C[直接返回快取結果]
    B -->|無或已過期| D[調用Currency API]
    D --> E[獲取最新匯率]
    E --> F[更新Redis快取<br/>TTL: 1小時]
    F --> G[返回結果給電商平台]
    
    H[每日同步完成] --> I[清除相關快取]
    I --> J[強制更新快取]
    
    style A fill:#e1f5fe
    style C fill:#e8f5e8
    style F fill:#fff3e0
    style I fill:#ffebee
```

## 4. 系統優勢與商業價值

### 4.1 核心優勢
- ✅ **自動化**: 每日自動同步，無需人工干預
- ✅ **可靠性**: 多重重試機制，確保資料準確性
- ✅ **即時性**: 提供最新匯率，支援即時轉換
- ✅ **擴展性**: 支援12種主流貨幣，可輕鬆擴展
- ✅ **監控**: 完整的日誌記錄和健康檢查機制

### 4.2 商業價值
- 🌍 **國際化支援**: 讓電商平台輕鬆進軍國際市場
- 💰 **提升轉換率**: 用戶看到本地貨幣價格，更容易下單
- 🔧 **降低維護成本**: 自動化同步，減少人工作業
- 📊 **數據準確**: 官方匯率來源，確保計算準確性
- ⚡ **效能優化**: 資料庫快取，提供快速查詢服務

### 4.3 實施時程建議

| 階段 | 工作項目 | 時程 | 負責單位 |
|------|----------|------|----------|
| 第一階段 | API整合測試 | 1週 | 技術團隊 |
| 第二階段 | 前端多幣別顯示 | 2週 | 前端團隊 |
| 第三階段 | 購物車轉換功能 | 1週 | 全端團隊 |
| 第四階段 | 訂單系統整合 | 1週 | 後端團隊 |
| 第五階段 | 效能優化與監控 | 1週 | DevOps團隊 |

## 5. 風險評估與應對策略

### 5.1 主要風險
- ⚠️ **外部API依賴**: tw.rter.info API可能不穩定
- ⚠️ **匯率波動**: 快速變動可能影響訂單價格
- ⚠️ **系統負載**: 高流量時可能影響響應速度

### 5.2 應對策略
- 🔄 **多資料源**: 考慮整合備用匯率來源
- 📝 **價格鎖定**: 在結帳時鎖定匯率一定時間
- 🚀 **效能優化**: 實施快取和CDN策略
- 📊 **監控告警**: 建立完整的監控和告警機制

---

*此文檔提供了完整的系統流程說明和電商平台整合方案，適合向上級報告使用。*
