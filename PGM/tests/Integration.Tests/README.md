# PGM.Integration.Tests

真實 DB／端到端煙霧測試。預設在**未設定連線字串**時，需 DB 的案例會 **Skip**（不是 Fail），因此本機或 CI 執行 `dotnet test` 不會因缺 SQL Server 而全紅。

## 離線可跑

`OfflineSmokeTests` 不啟動 Api、不連資料庫，永遠可執行。

## 啟用需 DB 的測試

設定與 Api／IIS 相同的環境變數（擇一）：

```powershell
# 建議（與正式環境鍵名一致）
$env:ConnectionStrings__DefaultConnection = "Server=...;Database=...;Trusted_Connection=True;TrustServerCertificate=True"

# 或僅測試用別名
$env:PGM_INTEGRATION_CONNECTION = "Server=...;Database=...;..."

dotnet test tests/Integration.Tests
```

測試宿主會以記憶體設定注入測試用 JWT（見 `IntegrationTestSettings.TestJwtSecret`），無需本機 User Secrets。

## 目前涵蓋

| 測試 | 條件 | 說明 |
|---|---|---|
| `OfflineSmokeTests` | 無 | 設定／Skip 說明文件化 |
| `HealthEndpointTests` | 需 DB | `GET /api/Health` 應回 200 且 `Healthy` |
