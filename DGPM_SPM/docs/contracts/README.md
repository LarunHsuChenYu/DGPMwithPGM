# DGPM Auth 設定（一律外連 PGM）

契約：姊妹專案 [`d:\07-DGPM\PGM\docs\contracts\auth-consumer-contract.md`](../../PGM/docs/contracts/auth-consumer-contract.md)

> **Local Auth 已退役**：DGPM 不再支援以本地 Auth DB 登入，也不再提供帳號／角色／功能選單／登入歷程維護 UI／API。上述主責為 **PGM**（系統權限管理平台）。DGPM 只吃 PGM 核發的 **DGPM 業務選單**。

## 部署參數（Api `appsettings`／環境變數）

| Key | 說明 |
|---|---|
| `Auth__AllowPGMLoginEntry` | `true`／`false`（是否允許由 DGPM 登入頁進入） |
| `Auth__PgmBaseUrl` | PGM **Api**（例：`http://localhost:9528`） |
| `Auth__PgmWebBaseUrl` | PGM **Web**（側欄外連；Development 預設 `https://localhost:7230`；Server 例：`http://localhost:8965`） |
| `Auth__SystemCode` | 固定 `DGPM` |

Web 端：`Auth__AllowPGMLoginEntry`、`Auth__PgmWebBaseUrl`；實際登入轉發一律由 DGPM Api → PGM。

## 必對齊 JWT（與 PGM 相同）

必須與 PGM `JwtSettings` **完全相同**：

- `JwtSettings__Issuer`（PGM 預設 `PGM.Api`）
- `JwtSettings__Audience`
- `JwtSettings__SecretKey`（同一字串，≥32）

否則登入拿到的票驗證會失敗。

範例（PowerShell / IIS 環境變數）：

```text
Auth__AllowPGMLoginEntry=true
Auth__PgmBaseUrl=http://localhost:9528
Auth__PgmWebBaseUrl=http://localhost:8965
Auth__SystemCode=DGPM
JwtSettings__Issuer=PGM.Api
JwtSettings__Audience=PGM.Api
JwtSettings__SecretKey=<same-as-pgm>
```

聯調清單：PGM `docs/contracts/phase3-uat-checklist.md`

## 跨系統權限（Admin → DGPM 業務選單）

系統權限在 **PGM** 維護；DGPM 只吃 PGM 核發的 DGPM 業務選單。Seed 帳號 **`Admin`（系統管理員）** 掛 `PGMAdmin`＋`DGPMAdmin`：以 `systemCode=DGPM` 登入預設 `DGPMAdmin` → **業務模組全開**（含 `RoleKPIList` 掛在經銷商KPI管理下）。**不再**側欄顯示「系統權限管理／帳號與角色維護」——請直接開 PGM Web。非管理員未授權 → 側欄空可接受。**不**復辟帳號／角色／FunctionList。

## 側欄空白時

選單真相在 **PGM** `SET_FUNCTION`（`SYSTEM_CODE=DGPM`）＋`MAP_ROLE_FUNCTION`。若登入成功但無業務模組：

1. 帳號須有 **`DGPMAdmin`**（僅 `PGMAdmin`／舊 `ADMIN` 不夠）。
2. 重跑 [`PGM/SQL/90_dev_seed_admin.sql`](../../PGM/SQL/90_dev_seed_admin.sql)。
3. JwtSettings 與 PGM 對齊；清 session 重登。

### `DGPMAdmin` 側欄預期（父層 → 子項）

| 父層 | 子項 |
|---|---|
| 基本資料管理 | 經銷商設定管理、區域組織管理 |
| 系統參數管理 | 匯率參數設定 |
| 經銷商KPI管理 | KPI 指標設定、KPI 數據匯入、KPI 數據覆核與解鎖、**KPI 資料權限設定** |
| 系統資料查詢 | KPI 異動紀錄查詢、KPI 匯入日誌查詢 |
| 經銷商儀錶板 | Qlik Cloud |

## Auth 行為摘要

| 項目 | 行為 |
|---|---|
| 登入／登出／Refresh／Me／Menus／切換角色／改密 | DGPM Api 轉發至 PGM |
| 側欄 | 以 PGM `/api/auth/menus` 為真相組樹 |
| 帳號／角色／功能／登入歷程維護 | **只在 PGM**（直接開 PGM Web；`PgmAuthLink` 已不掛 DGPMAdmin） |
| KPI 資料權限／參數／匯率 | DGPM 業務功能（`RoleKPIList` 父層＝`KPIIndicator`） |
