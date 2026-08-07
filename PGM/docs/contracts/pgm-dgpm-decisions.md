# PGM↔DGPM 聯調定案（決策紀錄）

> 定案日：2026-08-05。  
> 衝突時優先序：使用者當前指示 → 本文件 → [`AGENT_CONSTITUTION.md`](../../AGENT_CONSTITUTION.md) → 領域 `domain/` → 本目錄契約。  
> JWT／Login API 細節以 [`auth-consumer-contract.md`](auth-consumer-contract.md) 為兩專案共用規格。  
> 資料範圍（Dealer／KPI）定義見 [`data-scope-emp-org.md`](data-scope-emp-org.md)（本 Phase **不實作**範圍表）。

## 系統角色

| 系統 | 責任 |
|---|---|
| **PGM** | 帳號、角色、功能權限、登入驗證、系統參數代碼的**主責**系統 |
| **DGPM** | 業務資料管理系統；`AuthMode=PGM` 時不自建／自寫 Auth DB |

## 前提（已定）

1. 帳號／角色／參數主責＝**唯 PGM**。
2. DGPM 驗**同一套 JWT**（簽章以 PGM 為主）。
3. 業務選單須進 `SET_FUNCTION` 才顯示。
4. DGPM 不再自建／自寫 Auth DB（外連模式）。
5. PGM 不可用則 DGPM **不可登入**。

### DGPM 部署參數

| 參數 | 值 | 說明 |
|---|---|---|
| `AuthMode` | `Local`／`PGM` | `Local`＝DGPM 自建（過渡）；`PGM`＝外連 PGM |
| `AllowPGMLoginEntry` | `True`／`False` | `True`＝可由業務系統登入（期初）；`False`＝不允許（營運後） |

外連模式（`AuthMode=PGM`）：由 DGPM 登入入口進入，**PGM 控管**帳密與授權。

---

## 定案表（Q1～Q15）

| # | 題目 | 定案 | 摘要 |
|---|---|---|---|
| 1 | 開發／測試從哪裡登入 | **A** | 只開 DGPM 登入頁（背後呼叫 PGM）；帳號管理另開 PGM Web `http://localhost:8965`。`AuthMode`／`AllowPGMLoginEntry` 控管自建／外連與可否由業務登入 |
| 2 | ParamSet | **A** | 單一字典在 PGM；DGPM 業務只讀；必要代碼用 Seed 建在 PGM（不用舊 DGPM 參數） |
| 3 | 業務功能進 SET_FUNCTION | **A** | 新功能直接在 PGM Seed／SQL 建碼（可沿用現有／規劃中 Fun_ID） |
| 4 | 角色模型（測試期 Seed） | **A** | 以 PGM 角色為準；業務角色預設不可進 PGM 管理（AUTH01～04）。見下方角色表 |
| 5 | 角色×功能測資 | **A** | 不做舊 RIGHT 轉換；在 PGM RoleFunction／Seed 重勾／重種 |
| 6 | DGPM 側欄 | **A** | DGPM 只顯示業務選單；帳號／角色／參數到 PGM 維護 |
| 7 | 選單父層 | **A** | 授權只管葉功能 P；任一子項有權則父模組 M **自動顯示** |
| 8 | 停用／改角色失效時間 | **B** | 5～15 分鐘內；AccessToken **10 分鐘**＋定期向 PGM 檢核即可 |
| 9 | 登入紀錄 | **A** | 只寫 PGM；DGPM 只記業務 Audit（KPI 匯入／覆核／解鎖等）。查登入紀錄：串 PGM API 或導向 PGM |
| 10 | 組織／EMP_ORG／KPI 範圍 | **B** | 聯調前定義方向：`EMP_ORG` **不足**表達 Dealer／上傳範圍；DGPM 可有業務資料範圍表，主登入與角色仍由 PGM 控管。細節見 [`data-scope-emp-org.md`](data-scope-emp-org.md) |
| 11 | systemCode／多系統隔離 | **B** | 一開始帶 `systemCode`：允許值 **`PGM`／`DGPM`**。`DIM_ROLE`／`SET_FUNCTION` **加欄**；`MAP_ROLE_FUNCTION` **不加欄**（見下方隔離規則） |
| 12 | AUTH05 系統報表 | **A** | 維持佔位／**不納入**聯調驗收 |
| 13 | SET_FUNCTION／DIM_ROLE 維護 | **B → 已落地** | `FunctionList`（`/Permission/FunctionList`）、`RoleMaster`（`/system/role-master`） |
| 14 | 聯調驗收範圍 | **A** | 見下方驗收清單 |
| 15 | JWT／Login 錯誤碼規格 | **★** | 由 PGM 出共用規格；兩專案共用 → [`auth-consumer-contract.md`](auth-consumer-contract.md) |

---

## 角色表（測試期 Seed 目標）

| ROLE_ID | 說明 | SYSTEM_CODE | 預設不可含 |
|---|---|---|---|
| `PGMAdmin` | PGM 管理者 | `PGM` | —（可含 AUTH01～06／RoleMaster） |
| `DGPMAdmin` | DGPM 管理者 | `DGPM` | AUTH01～04（PGM 管理） |
| `DGPMUploader` | KPI Excel 上傳與匯入 | `DGPM` | AUTH01～04 |
| `DGPMReviewer` | KPI 覆核與解鎖 | `DGPM` | AUTH01～04 |

> Seed／維護 UI 已落地，見文末「落地狀態」。聯調請依 [`phase3-uat-checklist.md`](phase3-uat-checklist.md)。

---

## systemCode 隔離規則

| 物件 | 規則 |
|---|---|
| `DIM_ROLE.SYSTEM_CODE` | `VARCHAR(20) NOT NULL`，預設 `PGM`；允許 `PGM`｜`DGPM` |
| `SET_FUNCTION.SYSTEM_CODE` | 同上 |
| `MAP_ROLE_FUNCTION` | **不加欄**；隔離靠角色所屬 system＋功能所屬 system |
| Login `systemCode` | 缺省＝`PGM`；DGPM 固定傳 `DGPM` |
| JWT claim | `sys`（見契約） |
| RoleFunction 寫入 | 應拒絕跨系統勾選（角色 system ≠ 功能 system） |

**產品規則：**系統權限在 **PGM** 維護；DGPM 只吃 PGM 核發的 DGPM 業務選單。Seed **`Admin`** 同時掛 `PGMAdmin`＋`DGPMAdmin`：`systemCode=DGPM` → 業務模組全開；`RoleKPIList` 掛在 `KPIIndicator`；**不**再掛 `PgmAuthLink`（帳號／角色請直接開 PGM Web）。非管理員未授權 → 側欄空可接受。**不**在 DGPM 復辟帳號／角色／FunList。

### DGPM 側欄與 PGM 的分工

| 項目 | 所在 | 說明 |
|---|---|---|
| 帳號／角色／功能／登入紀錄 | **PGM**（AUTH*） | 系統權限管理平台本身；請直接開 PGM Web |
| 經銷商／KPI／匯率等 | **DGPM** 業務選單 | `SYSTEM_CODE=DGPM` |
| KPI 資料權限設定 | DGPM 業務葉 `RoleKPIList`（父層 `KPIIndicator`） | 業務資料範圍，非平台帳號維護 |
| `PgmAuthLink` | Fun 定義保留但軟刪／不授權給 `DGPMAdmin`／Uploader／Reviewer | 勿出現在一般 DGPM 側欄 |

DDL：[`SQL/10_dbo_pgm_tables.sql`](../../SQL/10_dbo_pgm_tables.sql)、既有庫升級 [`SQL/20_dbo_system_code.sql`](../../SQL/20_dbo_system_code.sql)。

---

## 聯調驗收清單（開發期，Q14）

### Auth／權限

- [ ] DGPM 登入頁呼叫 PGM Login API
- [ ] 登入成功
- [ ] 登入失敗（錯密／停用 → `AUTH_INVALID`）
- [ ] 修改密碼
- [ ] 帳號停用後不可登入
- [ ] 角色變更後 DGPM 選單變化
- [ ] JWT 過期處理
- [ ] 未授權功能不可進入

### 代表業務模組

- [ ] KPI Excel 上傳
- [ ] KPI 匯入預覽
- [ ] KPI 匯入紀錄查詢
- [ ] KPI 覆核／解鎖
- [ ] 呼叫 PGM Login／登入紀錄 API 查看登入記錄

### 不納入

- AUTH05 系統報表
- Dealer／KPI 資料範圍過濾（Q10 另案）
- （Q13 維護 UI **已落地**，納入 PGM 側驗證即可）

---

## Open Questions

| 項目 | 狀態 | 備註 |
|---|---|---|
| Dealer 可見／上傳範圍表結構與 UI | 另案 | 見 `data-scope-emp-org.md`；聯調以功能授權通過即可操作測資為準 |
| SET_FUNCTION／DIM_ROLE 簡易維護 UI | **已落地** | `FunctionList`（`/Permission/FunctionList`）、`RoleMaster`（`/system/role-master`）；Seed 功能 AUTH06 等 |
| 業務 Fun_ID 最終清單（KPI 各頁） | **Seed 已種** | 見 `90_dev_seed_admin.sql`；若 URL 變更再調 |
| Param 只讀 API 給 DGPM 的契約補充 | 若聯調需要再補 | 字典主責仍在 PGM |
| 與 QMS「同樣加密」 | 未定 | 憲法：未定案前各自獨立 |

---

## 落地狀態（2026-08 更新）

| 項目 | 狀態 |
|---|---|
| Seed：`PGMAdmin`／`DGPMAdmin`／`DGPMUploader`／`DGPMReviewer`＋KPI Fun＋`MAP_*`；測帳 `AshtonHsu`／`Admin` | **已落地**（`90_dev_seed_admin.sql`） |
| `SYSTEM_CODE` 欄位與既有列預設 | **已落地**（`10`／`20`＋Seed） |
| 選單父層 M 自動帶出 | **已落地**（`MenuRepository`） |
| JWT claim `sys`；`AUTH_INVALID`／`AUTH_NO_ROLE` | **已落地** |
| AccessToken **10 分鐘** | **已落地**（`Api/appsettings.json`） |
| 維護 UI：FunctionList／RoleMaster | **已落地** |
| RoleFunction 跨 system 檢核 | 實作時應拒絕；聯調時驗證 |
| Param 只讀契約補充 | 待需要時再補 |

### 聯調前請執行

1. PGM DB：`10_dbo_pgm_tables.sql` → `20_dbo_system_code.sql` → `90_dev_seed_admin.sql`
2. DGPM：`Auth__AuthMode=PGM`，且 `JwtSettings` Issuer／Audience／SecretKey **對齊 PGM**（見 DGPM `docs/contracts/README.md`）
3. 依 [`phase3-uat-checklist.md`](phase3-uat-checklist.md) 打勾

---

## 相關文件

| 文件 | 用途 |
|---|---|
| [`auth-consumer-contract.md`](auth-consumer-contract.md) | JWT Claim／Login／錯誤碼／DGPM Auth 參數 |
| [`data-scope-emp-org.md`](data-scope-emp-org.md) | EMP_ORG 不足與 DGPM 範圍表方向 |
| [`phase3-uat-checklist.md`](phase3-uat-checklist.md) | 聯調驗收打勾清單 |
| [`domain/Login.md`](../../domain/Login.md) | 登入領域 |
| [`SQL/_SQL_README.md`](../../SQL/_SQL_README.md) | DDL／執行序 |
