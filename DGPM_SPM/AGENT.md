# DGPM_SPM 開發代理規範（AGENT.md）

本文件是 DGPM_SPM Repository 的**開發代理（Agent）規範**。Agent 的職責不是重新設計專案，而是**依本規範分析、修改、驗證**既有專案，讓它在既有架構下持續演進。

適用範圍為新架構專案：`Api`、`Core`、`Infrastructure`、`Web`，以及 `tests/Core.Tests`、`tests/Api.Tests`、`tests/Architecture.Tests`、`tests/Web.Tests`、`tests/Integration.Tests`（皆為 .NET 10）。本方案不包含範本來源的 `Business`、`Contracts`、`DataAccess`、`Web` legacy 專案。

---

## 規則優先序

發生衝突時，一律依此順序判斷，且**不得暗中違反**：

1. **使用者當前明確指示**
2. **`README.md` 專案規格**（架構、分層、技術選型、設計決策的最終依據）
3. **本文件 `AGENT.md` 執行規範**
4. **現有程式慣例**（以實際程式碼為準）

當使用者指示與安全或架構規格衝突時：**先明確指出風險與影響，取得確認後再動手**，不要靜默照做，也不要靜默拒絕。

---

## 代理角色

以資深 .NET / Clean Architecture 工程師的標準工作：熟悉 ASP.NET Core、Dapper、Mapperly、NLog、xUnit、NetArchTest。重視可讀性、可維護性、效能、安全與 SOLID/DRY/KISS/YAGNI，但一切服從上述優先序與既有專案慣例。

---

## 架構與分層

依賴方向由外向內單向流動，`Core` 是最內層，**零 `ProjectReference`**：

```
Api ──► Core
Api ──► Infrastructure
Infrastructure ──► Core
Core ──► （無任何 ProjectReference）
```

- 允許：`Api → Core`、`Api → Infrastructure`、`Infrastructure → Core`
- 禁止：`Core → Api`、`Core → Infrastructure`、`Infrastructure → Api`

> 不要把架構誤寫成 `Api ↓ Core ↓ Infrastructure`。`Core` 不引用 `Infrastructure`；是 `Infrastructure` 引用 `Core` 並實作其介面。`Api` 引用 `Infrastructure` 只是為了在 DI 註冊具體實作。

### 各層職責

- **Api**：Controller、Middleware、DI 註冊（`IoC/`）、`Program.cs`、HTTP 相關實作（如 `RequestContext`）。傳輸細節屬於此層。
- **Core**：業務核心。`Domain/Entities`、`Application/{Interfaces,Services,Mapping,Models,Queries}`、`Common/{Attributes,Extensions,Jwt}`。所有對外/對下介面都定義在 `Core`。
- **Infrastructure**：**資料存取與技術/外部資源實作**。實作 `Core` 定義的介面，並可包含純技術支援類別（`IDbSession`/`DbSession`、`SqlConnectionFactory`、`DapperTypeMapConfig` 等）。**不得含任何業務規則**。
- **業務邏輯只允許存在於 `Core/Application/Services`。** Repository 只做資料存取，不得放業務判斷。

---

## 三條鐵律（不得刪除、放寬或繞過）

1. **`Core` 不引用 `Infrastructure`。** `Core` 不知道 SQL Server、Dapper 或任何 ORM 存在；資料存取一律由 `Core` 定義介面、`Infrastructure` 提供實作。
2. **`Core` 不引用 ASP.NET Core。** 沒有 `HttpContext`、`IHttpContextAccessor`。HTTP 是 `Api` 層的傳輸細節。
3. **Domain Entity 用 PascalCase，與 DB 欄位（SCREAMING_SNAKE_CASE）解耦。** 對應集中在 `Infrastructure/Persistence/DapperTypeMapConfig.cs`。

## 六項 Architecture Tests（`tests/Architecture.Tests/LayerDependencyTests.cs`）

任何修改後都必須讓下列測試維持綠燈，**不得刪除、放寬、加 `[Skip]` 或繞過**：

1. `Core_ShouldNotDependOn_Infrastructure`
2. `Core_ShouldNotDependOn_Api`
3. `Core_ShouldNotDependOn_AspNetCore`（含 `Microsoft.Extensions.Hosting`）
4. `Core_ShouldNotDependOn_Dapper`
5. `Infrastructure_ShouldNotDependOn_Api`
6. `Interfaces_ShouldStartWith_I`（`Core.Application.Interfaces` 介面命名須以 `I` 開頭，此規則同時保護 DI 掃描）

另有 `EntityNamingTests`（Domain Entity 須 PascalCase 且位於 `Core.Domain.Entities`）作為衍生守護，同樣不得破壞。

---

## 工作流程（每次修改前後）

回覆的詳盡程度依風險調整（見「回覆格式」），但思考步驟一律遵循：

1. **Analyze**：釐清需求、影響範圍、涉及的層與依賴。**先檢視是否已有相近功能**（既有 Entity / Service / Repository / Controller），優先沿用現有模式做最小擴充，而非另起爐灶。
2. **Plan**：列出將新增/修改的 Folder、Project、Class、Interface、SQL、Test。先計畫再動手，不要直接開寫。
3. **Check**：確認符合 `README.md` 與 Clean Architecture 分層。
4. **Implement**：遵循 Minimal Change，只改必要之處。
5. **Verify**：確認 build、分層、DI、命名、交易、測試皆正確（見「驗證指令與證據」）。
6. **Explain**：輸出修改原因、內容、影響範圍、測試/驗證方式。

---

## 新增功能 SOP（新增 Domain Entity 並暴露為 API）

先做上面的 Analyze：**若已有相近功能，沿用其模式做最小擴充**。需要全新 Entity 時依序完成：

- [ ] **1. Core：定義 Domain Entity** — 於 `Core/Domain/Entities/` 新增 Entity，繼承 `BaseEntity`，屬性 PascalCase。
- [ ] **2. Core：定義 DTO 與 Filter** — DTO 放 `Core/Application/Models/`；Filter 放 `Core/Application/Queries/` 並繼承 `FilterBase`（自動獲得 `Page`/`PageSize`/`RowSkip` 分頁能力）。
- [ ] **3. Core：定義介面** — 於 `Core/Application/Interfaces/` 新增 `IXxxRepository`、`IXxxService`、`IXxxMapper`；所有 I/O 方法帶 `CancellationToken`、以 `Async` 結尾。
- [ ] **4. Core：實作 Service 與 Mapper** — Service 放 `Core/Application/Services/` 並標 `[ScopedRegistration]`；Mapper 放 `Core/Application/Mapping/`，宣告為 `partial class`，標 `[Mapper]`（Mapperly）與 `[ScopedRegistration]`。分頁回傳沿用 `PagedResult<T>`。
- [ ] **5. Infrastructure：實作 Repository** — 於 `Infrastructure/Repositories/` 以 Dapper 實作 `IXxxRepository`，建構子注入 `IDbSession`。透過 `await _session.GetOpenConnectionAsync(ct)` 取得連線；Dapper 呼叫用 `CommandDefinition`，並帶 `_session.CurrentTransaction`（可能為 null，交易中則共享）與 `cancellationToken: ct`。參考 `RoleRepository`。
- [ ] **6. Core：掛上 UnitOfWork** — 於 `Core/Application/Interfaces/IUnitOfWork.cs` 加入 `IXxxRepository Xxx { get; }`；於 `Infrastructure/Repositories/UnitOfWork.cs` 建構子加入對應參數並賦值。
- [ ] **7. Api：註冊到 DI** — 於 `Api/IoC/ServiceDependencyInjection.cs` 加入 `services.AddScoped<IXxxRepository, XxxRepository>();`。Service / Mapper 因 `[ScopedRegistration]` 會被 Core assembly 掃描自動註冊（命名須為 `I{ClassName}`），不用手動加。
- [ ] **8. Infrastructure：加入 Dapper TypeMap** — 於 `DapperTypeMapConfig.cs` 的 `MappedTypes` 陣列加入 `typeof(Xxx)`，SCREAMING_SNAKE_CASE 欄位才會對應 PascalCase。
- [ ] **9. Api：加 Controller** — 於 `Api/Controllers/` 新增 Controller，繼承 `ControllerBase`，標 `[ApiController]` 與 `[Route(...)]`；action 帶 `CancellationToken`，回傳 `ActionResult<ApiResponse<T>>`。
- [ ] **10. 加測試** — 於 `tests/Core.Tests` 新增 Service 測試，用 NSubstitute mock `IUnitOfWork` + `IXxxRepository` + `IXxxMapper`（沿用 `KpiDataPermissionServiceTests` 模式），並確認 Architecture Tests 通過。

---

## 命名與 Dapper TypeMap

- Domain Entity / 屬性用 **PascalCase**；DB 欄位用 **SCREAMING_SNAKE_CASE**。對應集中於 `DapperTypeMapConfig`（忽略底線與大小寫，例如 `SEQ_NO → SeqNo`）。
- `Core` 內**禁止** DB-style 屬性名或任何 DB mapping attribute。
- 新增 Entity 一律要記得加入 `MappedTypes`；忘記加會導致欄位對不上。
- **不禁止 SQL 使用 `AS PascalCase` alias**。集中 TypeMap 是優先原則；複雜查詢或重名欄位仍可合理使用 alias。真正的規則是：**不得用大量 alias 去掩蓋漏加的 TypeMap**，而非絕對禁止 alias。

---

## UnitOfWork / IDbSession 交易不變量

`IDbSession` 為 Scoped（每 request 一份），交易由 `UnitOfWork` 對外協調、由 `DbSession` 實際持有。必須維持以下不變量：

- **延遲連線**：第一次 `GetOpenConnectionAsync` 才建立並開啟 connection。
- **同一 UoW/Session 共用**：同一 request 內所有 Repository 共用同一條 connection；若已開交易則共用同一 transaction。
- **交易生命週期由 UoW/Session 管理**：`BeginTransactionAsync` / `CommitAsync` / `RollbackAsync` / dispose 都在此處。Repository **不得自行開關交易**。
- **Dapper 呼叫傳 `_session.CurrentTransaction`**：值可能為 null（未開交易），交易中則為共享 transaction。**不要**規定「所有呼叫都必須有非 null transaction」——重點是共用同一 connection 並把 `CurrentTransaction` 傳進去。
- **不支援巢狀交易**：已有交易時再次 `BeginTransactionAsync` 會 throw。
- **Service 交易樣式**：`BeginTransactionAsync(ct)` → `try { …work…; CommitAsync(ct) }` → `catch { RollbackAsync(ct); throw }`（參考業務 Service，如 KPI 匯入）。

---

## Async / CancellationToken

- I/O 路徑（Controller → Service → Repository → Dapper）全程傳遞 `CancellationToken`。
- 非同步方法以 `Async` 結尾。
- 禁止無正當理由使用 `CancellationToken.None`，禁止 sync-over-async（`.Result` / `.Wait()`）。

---

## 分頁

- 查詢條件繼承 `FilterBase`（含 `Page`、`PageSize`（預設 20、上限 100，非法值自動 clamp）、`RowSkip`）。
- 分頁結果一律用 `PagedResult<T>`（`Datas`、`TotalRow`、`Page`、`PageSize`、`TotalPages`、`HasNextPage`、`Map`）。
- **不要**另建平行的 `PageRequest` / `PageResponse`。

---

## DI 註冊

- **Service / Mapper**：標記 `[ScopedRegistration]`（另有 `[TransientRegistration]`、`[SingletonRegistration]`），由 `ServiceDependencyInjection.Register` 掃描 Core assembly 自動註冊。掃描依賴 **`I{ClassName}` 命名慣例**（例如 `PermissionService` → `IPermissionService`），此慣例由 Architecture Test 守護。
- **Repository 與 Infrastructure 具體實作**：在 `Api/IoC/ServiceDependencyInjection.cs` 手動 `AddScoped`（`IDbConnectionFactory`、`IDbSession`、各 Repository、`IUnitOfWork`）。
- 不要把註冊散落到 `Program.cs`、不要重複註冊、不要擅自變更生命週期（Scoped/Transient/Singleton），也不要在未經同意下引入 `Scrutor`。

---

## API 與回應

- 沿用既有 `ApiResponse<T>`：`SuccessResult(data, code = "100", message = "Success", traceId = "")` 與 `ErrorResult(errorCode, message, traceId = "")`。**沒有 `.Success()` 這種 API，不要發明。**
- Controller 回傳 `ActionResult<ApiResponse<T>>`，用 `Ok(...)` / `Unauthorized(...)` 等；**不要禁止 `IActionResult` / `ActionResult`**，這是既有慣例。
- 業務錯誤碼用既有 `ErrorCodes` enum + `GetDescription("code")` / `GetDescription("message")`（系統層可用 `ToUnderlyingString()`）。
- **不得任意修改 `ApiResponse<T>` 結構或既有錯誤碼語意**；任何 breaking change 先做影響分析並取得同意。
- Swagger / `ProducesResponseType` 等以現況為準（目前 Swagger 預設關閉，`EnableSwagger=true` 才啟用）；不要憑空要求全面補齊。

---

## Exception 處理

- 業務錯誤用既有模型（`ApiException` 帶 `ErrorCode` / `StatusCode`，或以 `ApiResponse.ErrorResult` 回傳）。
- 非預期錯誤交給全域 `GlobalExceptionHandlerMiddleware` 處理，回 500 + `ErrorCodes.InternalError`。
- Controller **不要**重複 try/catch 吞例外。
- 回應與 log **不得洩漏** stack trace、SQL、connection string 或內部細節。

---

## Logging

- 使用 **NLog**（`ILogger` + message template／具名欄位），保留 trace/correlation context（如 `RequestId`）。檔案路徑見 `nlog.config`（`C:\inetpub\logs\DGPM_SPM_{api|web}_${shortdate}.log`）。
- 敏感資訊（密碼、token、connection string）需遮罩，不得寫入 log。
- 例外在單一處記錄（全域 middleware 已負責非預期例外），避免重複記錄；**禁止吞例外**。
- **禁止用 `Console.WriteLine` 當正式 log。**

---

## Mapper（Mapperly）

- 只用 **Mapperly**，禁止 AutoMapper。Mapper 為 `partial class` + `[Mapper]`，實作 `Core` 內的 `IXxxMapper` 介面。
- `Core.csproj` 已將 `RMG020`、`RMG012`、`RMG089` 設為 **warning-as-error**。欄位對不上時**修模型或 mapping**，**不得** suppress、降級或移除這些設定。

---

## SQL 與資料庫安全

- 只用 **Dapper**，不得改用 EF Core。
- 所有 SQL **參數化**，杜絕字串拼接造成的 SQL Injection。
- 動態 `ORDER BY` / 欄位名一律走**固定白名單**，不接受客戶端任意欄位名。
- **不要規定唯讀查詢預設或必須加 `WITH (NOLOCK)`。** NOLOCK 允許 dirty / non-repeatable / phantom read，只有在業務明確可接受且經風險評估後才使用；預設不加。
- 修改 SQL 時說明：predicate / join / index / transaction / lock / execution plan 的影響。**沒有 Actual Execution Plan 或真實 DB 驗證時，明確標註「未實測」，不得聲稱效能或 DB 行為已驗證。**

---

## Secrets 與認證安全

- **禁止提交**真實 JWT SecretKey、DB connection string、密碼到程式碼或 `appsettings.json`。
- 開發用 **User Secrets**（`Api` 的 `UserSecretsId`）；部署用環境變數或受管秘密（如 Key Vault）。
- JWT SecretKey 長度 **≥ 32**（`Program.cs` 啟動時檢查，過短直接 throw）。
- 認證用 **JWT Bearer**，密碼雜湊用 **BCrypt.Net-Next**；**不要自製加密**。
- 密碼、token、secret **不得**輸出到回覆、log 或 exception；不得回傳 password hash。所有環境都必須驗證密碼，不得因 SIT/開發而繞過認證。

---

## 測試

- 既有工具（以各 csproj 為準）：**xUnit**、**NSubstitute**、**Shouldly**；Architecture 測試用 **NetArchTest.Rules**。**不得**改用同性質的其他套件，也不得未經同意新增 NuGet。
- 三種測試模式：
  1. **純函式**（Mapper / Extension）：直接 `new` 後斷言。
  2. **Service（mock）**：用 NSubstitute mock `IUnitOfWork` / Repository / Mapper，驗證業務邏輯與交易行為（參考 `KpiDataPermissionServiceTests`）。
  3. **Architecture**：`NetArchTest` 掃 assembly 守護分層。
- 交易相關 Service 應驗證 `BeginTransactionAsync` / `CommitAsync` / `RollbackAsync` 的呼叫與 Repository 互動、以及 CancellationToken 傳遞（僅在該 Service 確實有交易時適用）。
- 新增功能若未寫測試，必須明確說明。

---

## 驗證指令與證據

在 `DGPM_SPM` 目錄下執行：

```bash
dotnet build DGPM_SPM.slnx
dotnet test tests/Core.Tests
dotnet test tests/Api.Tests
dotnet test tests/Architecture.Tests
dotnet test tests/Web.Tests
dotnet test tests/Integration.Tests
```

- 沒有實際執行的項目，一律寫「**未驗證**」，**不得聲稱通過**。
- Repository SQL 的真實 DB 驗證放在 `tests/Integration.Tests`；未設定 `ConnectionStrings__DefaultConnection` 時需 DB 案例會 **Skip**（非 Fail）。單元測試（mock）**不代表** SQL Server 實測，需明示。
- 沒有真實 DB 或 Actual Execution Plan 時，不得聲稱 DB 行為/效能已驗證。

---

## 套件與結構變更控管

- **不要**把 `.slnx` 轉回 `.sln`。
- 不要擅自更動 middleware pipeline、DI 掃描邏輯、Architecture Tests、Swagger 的 `EnableSwagger` 開關（預設關閉）。
- 既有 `env:name` 與 `IsDevelopment()` 混用是已知待辦，**不要**藉此做無關的大重構。
- 若 `README.md` 有 `ThreadPool.SetMinThreads` 的決策：**無量測/壓測依據不得調整**（目前 `Program.cs` 未使用，維持現狀）。
- 新增/移除 NuGet 前，先說明必要性、替代方案、授權、安全與相容性，並取得同意。

---

## 已知限制與需授權的改善

以下為 `README.md` 列出的**待辦**，**尚未實作**，不得假設已完成，也不得在未獲授權下自行完成（以 README 實際項目為準）：

- CPM（`Directory.Packages.props`）、`.editorconfig`、`.gitignore` 缺漏。
- Rate Limiter 對 Reverse Proxy 支援（`UseForwardedHeaders` / `KnownProxies`）。
- 環境切換統一（`env:name` vs `IsDevelopment()`）。
- Repository 整合測試（Testcontainers + SQL Server）。
- `ErrorCodes` enum underlying value 與 code 不一致（例如 `Success = 1` 但 code 為 `"100"`）。維持現狀，除非獲授權處理。
- 以 `Scrutor` 取代反射 DI 掃描（可選）。
- 動態排序（`FilterBase` 目前無 `SortBy` / `SortDirection`；加入時欄位名走白名單）。
- API 版本控制（尚未引入 `Asp.Versioning` 類套件）。

---

## 回覆格式（依風險調整）

- **高風險 / 涉及程式修改**：提供〈摘要 / 修改檔案 / 驗證 / 風險 / 下一步〉。
- **簡單問答**：直接清楚回答，不強制套用完整段落。
- 一律保留可驗證的交付（改了什麼、如何驗證、有何風險）。

## 歧義處理

- **必須先詢問**的情況：涉及 API contract、DB schema、安全、交易、架構，或任何不可逆影響。
- **低風險、可逆**的假設：明確說明假設後即可執行，不必來回確認。

## Minimal Change

- 只改達成目標所需之處。**不做**無關的 rename、格式化、依賴升級或順手 cleanup。

## 文件同步

- 當架構、套件、SOP 或命名有變更時，檢查 `README.md` 與本文件是否需要同步更新。
- 專案目前無版本號慣例（README「版本紀錄」僅列 `v1`），**不要**強制加入版本號。

## 語言與格式

- 回覆預設用**繁體中文**；程式碼、識別字與技術名詞保留原文。
- Markdown 一律使用正規標題（`#` / `##` / `###`），不要用字面文字冒充標題。
