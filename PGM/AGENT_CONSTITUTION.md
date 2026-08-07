# AGENT_CONSTITUTION.md — PGM 開發規範（專案憲法）

> 本文件為 **PGM（獨立權限／系統管理平台）** 的 Agent 憲法。  
> 來源：公司級 Grok 憲法模板＋本專案 Clean Architecture 慣例＋獨立權限模組計畫定案。  
> 衝突時以本文件為專案內唯一判準（僅次於使用者當前指示與已核准 SA）。禁止未經說明即依較寬鬆規則執行。

## 0. 占位符與文件使用方式

- `{ProjectName}`：目前專案、Solution 或系統名稱（本專案＝PGM）。
- `{Entity}`：本次需求涉及的領域實體（如 Login、EmpUser、RoleFunction）。
- `{OwnerSystem}`：該資料的主責系統；本階段預設為 PGM。
- Agent 應先從 Solution、README、命名空間、`SQL/`、`domain/`、API Route 與既有程式碼解析實際值；無法可靠判斷時，列為待確認事項，**不得自行臆測**。
- 占位符僅用於文件模板，**不得**原樣帶入 Namespace、Route、SQL、資料表、欄位或正式交付內容。
- [`AGENT_IMPLEMENT.md`](AGENT_IMPLEMENT.md) 為 PG 實作 SOP；[`AGENT_ANALYSIS.md`](AGENT_ANALYSIS.md) 為 SA／高風險變更分析手冊。兩者僅能補充本文件，不得降低本文件要求。
- 領域規則位於根目錄 [`domain/`](domain/)；[`docs/`](docs/) 僅放 SA 原件（SRS／BMW），兩者勿混用。

## 1. 目的與適用範圍

適用於 PGM 之新開發、重構、API、資料庫異動、文件與測試產出。  
目標：可維護、資安合規、資料正確、低停機風險，並嚴格對齊 BMW LIST 與四份 PGM SRS。

適用專案：`Api`、`Core`、`Infrastructure`、`Web`，以及 `tests/*`（.NET 10）。

## 2. 規則優先順序（由高至低）

1. **使用者當前明確指示**
2. **已核准之 SA 規格**（`docs/PGM_Qlik_*.docx`、`docs/BMWv20260720.*`）、領域規則（`domain/*.md`）與**已核准聯調定案／契約**（`docs/contracts/`）
3. **本文件 `AGENT_CONSTITUTION.md` 鐵律**
4. **`README.md`／`SQL/_SQL_README.md` 技術、部署與表結構約定**
5. **`AGENT_IMPLEMENT.md`、`AGENT_ANALYSIS.md`**（僅在不違反前四項時適用）
6. **現有程式慣例**（以實際程式碼為準）
7. 一般業界最佳實務（僅供補充，非優先）

當使用者指示與資安／架構衝突時：**先指出風險與影響，取得確認後再動手**。

## 3. 分層架構鐵律

依賴方向由外向內；`Core` 零 `ProjectReference`：

```text
Api ──► Core
Api ──► Infrastructure
Infrastructure ──► Core
Core ──► （無任何 ProjectReference）
Web ──►（HTTP）Api     ※ 禁止 Web 專案引用 Core／Infrastructure
```

- **Api**：Controller、Middleware、DI（`IoC/`）、JWT 管線、HTTP 細節（如 `CurrentUser`）
- **Core**：Domain Entity、Application Services／Interfaces／DTO／Mapperly；**唯一**允許業務規則之處
- **Infrastructure**：Dapper Repository、`DbSession`／UoW、TypeMap；**禁止**業務判斷
- **Web**：Blazor Server UI；僅經 `PgmApiClient`（設定鍵 `PgmApi:BaseUrl`）呼叫 Api

### 三條鐵律（不得刪除、放寬、Skip 或繞過）

1. `Core` 不引用 `Infrastructure`
2. `Core` 不引用 ASP.NET Core（無 `HttpContext`）
3. Domain Entity 用 PascalCase；DB 用 SCREAMING_SNAKE_CASE，對應集中於 `DapperTypeMapConfig`

### Architecture Tests（必須維持綠燈）

`tests/Architecture.Tests` 六項分層＋Entity 命名守護，**不得**刪除、放寬、加 `[Skip]`。

## 4. 資安與授權鐵律

- Api Controller／端點預設需 JWT Bearer；僅 login／health 等明確允許匿名
- Web 全域 Authorize；登入頁 `[AllowAnonymous]`
- **授權真相**：Application 層依角色×功能（`MAP_ROLE_FUNCTION`→`SET_FUNCTION`）檢查；UI 隱藏僅為提示，不可當唯一防線
- 密碼：BCrypt；流程為「查帳號 → 程式端 `Verify`」；封裝 `IPasswordHasher`；**禁止** SQL 明文等值比對密碼
- **Login SRS 範例 SQL 不採納**：SRS 示意之明文 `PASSWORD` 等值比對、以及字元型 `DEL_FLG='N'` **一律不採用**；以 BMW `DEL_FLG` bit（0 活動／1 停用）＋程式端 BCrypt Verify 為準。與 QMS「同樣加密」列為 Open Question；**未定案前各自獨立**（見 §6）
- 禁止僅依 URL／Route 參數判斷權限（防 IDOR）
- Secret（Jwt SecretKey、連線字串）不得進原始碼；走環境變數／User Secrets／IIS

## 5. 資料庫鐵律

- 參數化查詢；禁止字串拼接 SQL；禁止以 `NOLOCK` 當效能解法
- **表範圍以 BMW LIST 為準**；功能主檔表名＝**`SET_FUNCTION`**（不建 `SysFun`）
- **權限鏈（SRS）**：`EMP_USER` → `MAP_USER_ROLE`／`DIM_ROLE` → `MAP_ROLE_FUNCTION` → `SET_FUNCTION`
- **禁止**再引入 `MAP_ROLE_RIGHT`／`MAP_RIGHT_FUNCTION`／`DIM_RIGHT`／`MAP_ROLE_RUNCTION`（本階段）
- `SET_FUNCTION` 含 BMW LIST 欄＋**SysFun 擴充欄**（`PARENT_ID`、`ACTION_TYPE`、`IS_MENU`、`IS_ENABLED`、`FUN_DESC`、`ICON`；**超出 BMW LIST、專案定案**，利於日後 DGPM 對接）；階層以 `PARENT_ID` 為準，`PARENT_NAME` 供 SRS／顯示相容
- `EMP_ORG` 僅 DDL 預留，無維護 UI
- `SET_FUNCTION`／`DIM_ROLE`：簡易維護 UI **已落地**（`FunctionList`、`RoleMaster`）；仍可用 Seed／SQL 補資料
- 會被多人或多系統更新的資料，必須採用與既有架構一致的併發控制；BMW／本專案以 `DEL_FLG`＋稽核欄為主。若需 RowVersion／樂觀鎖，另案依風險分級停等後再加
- 複雜狀態轉移應在單一交易內執行；鎖定策略依競態條件評估，**不得盲目**加 `UPDLOCK`／`HOLDLOCK`
- DDL 變更屬高風險（見 §7）；腳本放 `SQL/`，idempotent 優先
- 軟刪依規格：`DEL_FLG` bit（0 活動／1 停用）

## 6. 本專案產品邊界

| 做 | 不做 |
|---|---|
| Login（含改密、角色切換、選單依角色；`systemCode`／JWT `sys`） | QMS 共用同一 `EMP_USER`（未定案前各自獨立） |
| EMPSet（帳號 CRUD、角色多選） | `DIM_RIGHT`／舊 RIGHT 鏈 |
| RoleFunctionSet（`MAP_ROLE_FUNCTION` 全量覆寫） | KPI／org／匯率等業務資料本體（屬 DGPM） |
| ParamSet（`SET_PARAM` CRUD；`SET_PARAMITEM` 只讀） | Dealer／KPI **資料範圍過濾**（定義見 contracts；本 Phase 不實作） |
| Phase 3：DGPM 外連 Auth／選單（依 `docs/contracts/`） | — |
| `SET_FUNCTION`／`DIM_ROLE` 簡易維護（FunctionList／RoleMaster） | — |

**Phase 3 聯調定案（2026-08-05）已核准**：帳號／角色／參數／登入主責＝唯 PGM；DGPM（`AuthMode=PGM`）依 [`docs/contracts/pgm-dgpm-decisions.md`](docs/contracts/pgm-dgpm-decisions.md) 與 [`docs/contracts/auth-consumer-contract.md`](docs/contracts/auth-consumer-contract.md) 對接。JWT／公開 API／權限語意變更仍屬 §7 高風險。

規格真相來源：`docs/`；執行現況：`SQL/`、`domain/`。

## 7. 風險分級與高風險變更停等機制

| 等級 | 範例 | Agent 行為 |
|---|---|---|
| 低風險 | 明確 Bug、文案、已存在流程的局部修正 | 可直接實作；交付時說明驗證結果。 |
| 中風險 | 內部查詢邏輯、可選欄位、索引建議、既有流程擴充 | 先列出假設、影響與驗證方式；需求已明確時可實作。 |
| 高風險 | 下列清單所列項目 | 必須依 `AGENT_ANALYSIS.md` 產出影響分析並停等確認，始可實作。 |
| 禁止自行執行 | 正式環境大量異動／刪除、不可逆轉換、關閉資安控制 | 僅能提供腳本、執行前提、備份與回復方案；未取得明確授權不得執行。 |

以下變更屬於**高風險**：

- 資料表結構、索引、預存程序簽名變更。
- 公開 API／JWT Contract 的 Request、Response 或 Claim 變更。
- 權限鏈或角色定義變更（含 `MAP_ROLE_FUNCTION`／`SET_FUNCTION` 語意）。
- 密碼演算法或 BCrypt Work factor。
- 大量資料遷移或歷史資料處理。
- 未來對外 Consumer（如 DGPM）契約變更。

停等期間的最低產出請依 [`AGENT_ANALYSIS.md`](AGENT_ANALYSIS.md) 辦理，至少包括影響範圍、相容性、回復計畫、驗證項目與待確認決策。

## 8. 交付與回覆格式（固定）

每次實作／重大分析回覆須含：

【結論】  
【修改檔案清單】  
【架構／分層影響】  
【資安與授權影響】  
【資料庫與交易影響】  
【驗證方式】  
【尚未驗證項目與風險】  
【回滾計畫】  
【Open Questions（若有）】

禁止僅貼程式碼或只寫「已完成」。

## 9. 模型與模式使用方式

- 所有模式均須遵守本文件優先順序。
- 實作模式（PG）建議搭配 `AGENT_IMPLEMENT.md`。
- SA 模式或高風險變更必須搭配 `AGENT_ANALYSIS.md`；需求確認後，實作時再搭配 `AGENT_IMPLEMENT.md`。
- 可依任務切換不同模型，但**不得**切換規範來源（一律以本專案根目錄文件為準）。

### 工作流程（每次修改）

1. **Analyze**：需求、影響層、風險等級；對照 `domain/` 與 SRS；高風險先走分析手冊
2. **Plan**：將改的專案／類別／SQL／測試
3. **Check**：符合本憲法與分層測試
4. **Implement**：最小變更；禁止無關重構
5. **Verify**：`dotnet build`／相關 `dotnet test`；DB 變更說明如何驗證
6. **Report**：依 §8 格式回覆

## 10. 衝突裁決條款

若本文件與 `AGENT_IMPLEMENT.md`、`AGENT_ANALYSIS.md`、README.md 或其他子規範產生衝突，以本文件為準；但已核准之專案需求、業務規則及主責系統定義，依第 2 節優先順序處理。

禁止未經說明即依較寬鬆之規則執行。
