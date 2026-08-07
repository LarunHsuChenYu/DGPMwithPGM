# DGPM 使用說明

> 依現行 Blazor 畫面與功能選單整理（經銷商績效管理系統）。  
> 測試機入口：Web `http://localhost:8964`；Api `http://localhost:9527`（健康檢查：`/api/health`）。  
> 帳號／角色／功能授權主責為 **PGM**（報表系統／權限平台）：`http://localhost:8965`。  
> 截圖日期：2026-08-07（含 PGM 系統權限管控、完整／受限權限首頁與各功能頁畫面）。  
> 安裝／部署請另見 [安裝文件.md](../安裝文件.md)。

---

## 1. 系統簡介

DGPM（Dealer Performance Management，經銷商績效管理系統）用於維護經銷商／區域基本資料、匯率參數、KPI 指標與數據（匯入、覆核、資料範圍），並提供異動／匯入日誌查詢與 Qlik Cloud 儀錶板入口。

**系統權限以 PGM 完成管控**：帳號、角色、功能授權皆在 PGM 維護；DGPM 登入後依 PGM 授予的功能選單顯示可用模組與首頁卡片。不同角色可見模組數量不同。

請勿混淆兩類權限：

| 類型 | 在哪裡設定 | 控制什麼 |
|---|---|---|
| **系統權限（PGM）** | PGM「使用者帳號維護」「角色權限設定」等 | 能否登入、可見哪些功能選單／模組 |
| **資料權限（DGPM）** | DGPM「KPI 資料權限設定」 | 能看哪些經銷商／區域的 KPI 資料 |

本系統主要功能對照：

| 功能 | 選單名稱 | 路徑 |
|---|---|---|
| 登入／登出 | （登入頁／右上登出） | `/login` |
| 經銷商設定管理 | 經銷商設定管理 | `/basic/dealers` |
| 區域組織管理 | 區域組織管理 | `/basic/regions` |
| 匯率參數設定 | 匯率參數設定 | `/parameters/exchange-rates` |
| KPI 指標設定 | KPI 指標設定 | `/kpi/indicators` |
| KPI 數據匯入 | KPI 數據匯入 | `/kpi/import` |
| KPI 數據覆核與解鎖 | KPI 數據覆核與解鎖 | `/kpi/review` |
| KPI 資料權限設定 | KPI 資料權限設定 | `/system/kpi-permissions` |
| KPI 異動紀錄查詢 | KPI 異動紀錄查詢 | `/query/kpi-changes` |
| KPI 匯入日誌查詢 | KPI 匯入日誌查詢 | `/query/import-logs` |
| 經銷商儀錶板 | Qlik Cloud | `/dashboard` |

權限鏈（概念）：PGM 帳號／角色 → 角色功能授權（含 DGPM 功能）→ DGPM 登入後載入左側選單與首頁模組。

> **說明**：帳號維護、角色權限設定、重設密碼、登入紀錄等作業請至 **PGM** 操作；DGPM 不再提供本機獨立帳號系統。詳細步驟見下方 [§2.5 系統權限管控（PGM）](#25-系統權限管控pgm)。

---

## 2. 登入

### 2.1 進入畫面

瀏覽器開啟 `http://localhost:8964`。未登入會導向 `/login`。

目前正式環境可允許由 **DGPM 登入頁**進入（帳密仍轉送 PGM 驗證，系統代碼固定為 `DGPM`）。亦可至 PGM 入口 `http://localhost:8965` 管理帳號與權限。

![DGPM 登入頁](images/dgpm-login.png)

上圖為 DGPM 本身登入頁（畫面標題為「DGPM／經銷商績效管理系統」）。

![PGM 登入畫面（無使用權限提示）](images/pgm-login-no-permission.png)

上圖為 PGM 登入頁：帳密正確但**無此系統可用角色／權限**時，會顯示「無使用權限，請聯絡系統管理員。」（截圖帳號欄為 CathyWang；更多說明見 §2.5.5。）

### 2.2 操作步驟

1. 輸入**帳號**、**密碼**（皆必填）。
2. 按 **登入**。

### 2.3 系統行為

| 情況 | 結果 |
|---|---|
| 帳密錯誤或帳號停用 | 提示帳號／密碼錯誤（或後端回傳之錯誤訊息） |
| 帳號無 DGPM 可用角色 | 拒登；請管理員至 PGM 為該帳號設定 DGPM 相關角色與功能 |
| 來源／權限不被允許 | 提示無使用權限，請聯絡系統管理員 |
| PGM 服務不可用 | 暫時無法登入（服務忙碌或連線失敗） |
| DGPM 登入入口已停用（`AllowPGMLoginEntry=false`） | 登入頁顯示「本系統登入入口已停用，帳號主責為 PGM，請至 PGM 系統登入。」 |
| 登入成功 | 進入系統首頁；左側選單依授權載入；右上顯示登入者名稱與 **登出** |

> **實作說明**：認證由 DGPM Api 轉發至 PGM；JWT 與 PGM 共用設定。開發／部署細節見 [安裝文件.md](../安裝文件.md)。

### 2.4 帳號與權限（於 PGM 維護）

**系統權限（帳號、角色、功能授權）必須在 PGM 完成管控**；DGPM 僅依授權結果顯示選單，不提供本機帳號管理。

帳號、角色、功能勾選請在 PGM 完成。PGM 側欄常見項目如下（實際項目依該帳號授權而定）：

![PGM 側邊選單／報表系統首頁](images/pgm-menu-home.png)

常見相關作業：

| PGM 功能 | 用途（對 DGPM） |
|---|---|
| 使用者帳號維護 | 建立／維護可登入帳號，並綁定角色 |
| 角色權限設定 | 勾選功能（含 DGPM 業務功能與 AUTH 系統管理功能），決定可見選單 |
| 重設密碼 | 變更登入密碼 |
| 登入紀錄 | 查詢登入歷程（DGPM 已不再提供登入軌跡查詢頁） |

> **注意**：僅在 PGM 有帳號、但未授權任何 DGPM 功能時，登入 DGPM 後可能出現「目前沒有可進入的模組」或拒登。請確認角色已勾選對應功能。操作示例見下一小節。

### 2.5 系統權限管控（PGM）

本節說明如何在 **PGM** 設定帳號角色與功能勾選，以及這些設定如何反映到 **DGPM** 首頁／側欄。  
（PGM 操作細節亦可對照 `PGM/docs/user-guide/PGM使用說明.md` 之「角色權限設定」「使用者帳號維護」。）

#### 2.5.1 系統權限 vs 資料權限

| | 系統權限（PGM） | 資料權限（DGPM） |
|---|---|---|
| 設定位置 | PGM：使用者帳號維護、角色權限設定 | DGPM：KPI 資料權限設定（見 §5.4） |
| 控制範圍 | 能否登入、可見哪些功能選單／模組卡片 | 能看哪些經銷商／區域的 KPI 資料 |
| 典型畫面 | 帳號綁角色、角色勾選功能代碼 | 依 USER_ID 勾選區域／加入經銷商 |

兩者需一併設定才完整：即使在 PGM 勾了「KPI 數據覆核」，若未在 DGPM 開通對應資料範圍，仍可能查不到預期資料。

#### 2.5.2 使用者帳號綁定角色

於 PGM 左側 **系統管理 → 使用者帳號維護**，可建立帳號並綁定一或多個角色。

![PGM 使用者帳號維護（角色欄）](images/pgm-user-accounts.png)

測試機常見對照（角色欄可見 DGPM管理者、PGM管理者、DGPM KPI覆核等）：

| 帳號 | 姓名 | 綁定角色（示例） | 預期結果（概要） |
|---|---|---|---|
| Admin／AshtonHsu | 系統管理員／許震宇 | **DGPM管理者**＋**PGM管理者** | DGPM 業務功能完整；且可進 PGM 做帳號／角色等 AUTH 作業 |
| CathyWang | 王碧如 | **DGPM管理者** | DGPM 業務功能完整；**不含** AUTH（無法在 PGM 管理他人權限） |
| JessieHu | 胡詠晴 | **DGPM KPI覆核** | DGPM 僅覆核／解鎖與相關查詢 |

#### 2.5.3 角色權限設定（功能勾選差異）

於 PGM 左側 **系統管理 → 角色權限設定**：選角色後勾選功能，按表頭 **確認** 儲存（全量覆寫）。

**DGPM管理者**：勾選 DGPM 業務功能（經銷商、區域、匯率、KPI 設定／匯入／覆核、**KPI 資料權限設定**、異動／匯入日誌、Qlik 等）；**AUTH01～AUTH08 皆未勾**。

![角色權限設定：DGPM管理者](images/pgm-role-perm-dgpm-admin.png)

**PGM管理者**：幾乎全勾（含全部 AUTH 與 DGPM／PGM 相關功能），負責平台帳號與權限治理。

![角色權限設定：PGM管理者](images/pgm-role-perm-pgm-admin.png)

**DGPM KPI覆核**：僅勾 `KPIImpReview`（KPI 數據覆核與解鎖）、`KPIChgLog`、`KPIImpLog`。

![角色權限設定：DGPM KPI覆核](images/pgm-role-perm-kpi-review.png)

**DGPM KPI上傳**：勾選 KPI 相關功能（含模組節點、指標設定、匯入、覆核、異動／匯入日誌）；通常**未**勾 `RoleKPIList`（KPI 資料權限設定）與 AUTH。

![角色權限設定：DGPM KPI上傳](images/pgm-role-perm-kpi-upload.png)

> **注意**：角色主檔（DIM_ROLE）與功能主檔由 IT／Seed／SQL 維護；本畫面只做「角色 ↔ 功能」勾選覆寫。重要角色變更前建議先確認影響對象。

#### 2.5.4 反映到 DGPM 首頁（完整 vs 受限）

PGM 勾選結果會決定 DGPM 左側選單與首頁模組。對照如下：

**許震宇（AshtonHsu，DGPM管理者＋PGM管理者）**— 五大模組完整；側欄含 **KPI 資料權限設定**（屬 DGPM 資料權限維護入口，授權本身仍來自 PGM 的 `RoleKPIList` 功能勾選）。

![DGPM 首頁（許震宇／完整）](images/dgpm-home-ashton.png)

**王碧如（CathyWang，DGPM管理者）**— 同樣為完整五大模組與側欄（含 KPI 資料權限設定）；與許震宇在 **DGPM 畫面**上相近，差異在於其未綁 PGM管理者，無法於 PGM 執行 AUTH 類系統管理。

![DGPM 首頁（王碧如／完整）](images/dgpm-home-cathy.png)

**胡詠晴（JessieHu，DGPM KPI覆核）**— 僅見 **經銷商KPI管理**（子功能僅「KPI 數據覆核與解鎖」）與 **系統資料查詢**（異動／匯入日誌）。

![DGPM 首頁（胡詠晴／受限）](images/dgpm-home-jessie.png)

#### 2.5.5 「無使用權限」情境

帳密正確，但帳號對該入口／系統**尚無可用角色或功能**時，PGM 登入頁會顯示「無使用權限，請聯絡系統管理員。」（下圖帳號欄為 CathyWang，作為錯誤提示示例。）

![PGM 登入：無使用權限](images/pgm-login-no-permission.png)

處理方式：請具 PGM管理者權限者至「使用者帳號維護」綁定適當角色，並確認「角色權限設定」已勾選目標系統所需功能。

---

> **示範說明**：本章節起為畫面示範，不屬現行 SRS 需求範圍。

## 3. 基本資料管理（示範）

### 3.1 經銷商設定管理

維護經銷商基本資料與啟停用狀態。

![經銷商設定管理](images/dgpm-dealers.png)

### 3.2 區域組織管理

維護區域階層與啟停用狀態。

![區域組織管理](images/dgpm-regions.png)

## 4. 系統參數管理 — 匯率參數設定（示範）

維護各幣別每月匯率。

![匯率參數設定](images/dgpm-exchange-rates.png)

## 5. 經銷商 KPI 管理（示範）

### 5.1 KPI 指標設定

維護 KPI 指標定義與啟停用。

![KPI 指標設定](images/dgpm-kpi-indicators.png)

### 5.2 KPI 數據匯入

以表單貼上方式匯入 KPI 數據並查看匯入結果。

![KPI 數據匯入（含匯入表單與最近匯入批次）](images/dgpm-kpi-import.png)

### 5.3 KPI 數據覆核與解鎖

覆核確認或解鎖 KPI 數據。

![KPI 數據覆核與解鎖](images/dgpm-kpi-review.png)

### 5.4 KPI 資料權限設定

依帳號設定可存取的經銷商／區域資料範圍。

![KPI 資料權限設定](images/dgpm-kpi-permissions.png)

## 6. 系統資料查詢（示範）

### 6.1 KPI 異動紀錄查詢

查詢 KPI 匯入、修改、覆核、解鎖等異動紀錄。

![KPI 異動紀錄查詢](images/dgpm-kpi-changes.png)

### 6.2 KPI 匯入日誌查詢

查詢 KPI 匯入批次日誌。

![KPI 匯入日誌查詢](images/dgpm-import-logs.png)

## 7. 經銷商儀錶板（Qlik Cloud）（示範）

嵌入 Qlik Cloud 儀錶板（未設定嵌入網址時顯示占位畫面）。

![經銷商儀錶板（未設定 EmbedUrl 之占位畫面）](images/dgpm-dashboard.png)

## 8. 常見問題

| 問題 | 建議處理 |
|---|---|
| 登入後選單／首頁模組很少 | 該角色於 PGM「角色權限設定」是否未勾選 DGPM 功能？對照 §2.5、§2.5.4 |
| 提示無使用權限 | 帳號在 PGM 無可用角色，或來源不被允許；請聯絡具 PGM管理者權限者（見 §2.5.5） |
| 無法連線伺服器 | 確認測試機 Web／Api：`http://localhost:8964`、`http://localhost:9527/api/health` |
| 忘記密碼／需改密 | 請至 PGM 使用「重設密碼」，或請管理員協助 |
| 登入入口已停用 | 環境關閉 DGPM 登入頁時，請改至 PGM 登入／聯絡 IT |
| §3～§7 畫面相關問題 | 該範圍僅供畫面示範、不屬現行 SRS；詳見各章截圖 |
