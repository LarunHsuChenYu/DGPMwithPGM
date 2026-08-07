# AGENT_IMPLEMENT.md — PGM 實作手冊

> 僅在不違反 [`AGENT_CONSTITUTION.md`](AGENT_CONSTITUTION.md) 時適用。對齊獨立權限平台計畫與 Clean Architecture。  
> 高風險變更須先完成 [`AGENT_ANALYSIS.md`](AGENT_ANALYSIS.md) 所定分析與確認。

## 1. 新增／調整功能十步驟

1. 讀取 `AGENT_CONSTITUTION.md`、README、已核准 SA（`docs/`），以及 `domain/{Entity}.md`（如有）；高風險變更先完成 `AGENT_ANALYSIS.md` 所定分析與確認
2. 在 `Core` 建立／調整 Entity、DTO、`I{Entity}Repository`、`I{Entity}Service`；需要時補 Mapperly Mapper
3. 在 `Core/Application/Services` 實作業務（建立、查詢、更新、軟刪、權限覆寫）
4. 在 `Infrastructure/Repositories` 實作 Dapper（參數化；對齊 BMW 欄位名）；Entity↔欄位對應寫入／確認 `DapperTypeMapConfig`
5. 在 `Api/Controllers` 暴露 REST；類別級 `[Authorize]`；匿名僅允許明確端點；必要時掛／調整 Filter（例外、授權、稽核等既有管線）
6. 在 `Web` 以最小變更新增／調整 Blazor 頁；經 `PgmApiClient` 呼叫，禁止直連 DB
7. 依既有資料庫慣例撰寫或更新 SQL／Repository；資料異動須具備交易邊界（UoW／`DbSession`）、併發控制（`DEL_FLG`＋稽核）、稽核與必要的重送保護；選單／授權走 `MAP_ROLE_FUNCTION`＋`SET_FUNCTION`（依角色過濾）
8. 若需 DDL：更新 `SQL/`，依風險分級停等確認後再執行；同步 `domain/` 與 `SQL/_SQL_README.md`；DI 註冊（`Api/IoC/`、Web `Program.cs`／HttpClient）一併完成
9. 單元測試（Core／Api）＋必要整合測試；Architecture Tests 必須綠
10. 依 `AGENT_CONSTITUTION.md` §8 交付格式回覆

### Template 關鍵掛點（易漏）

新增或改寫功能時，除上述步驟外須逐項確認 Template／骨架掛點未漏：

| 掛點 | 典型位置 | 檢查 |
|---|---|---|
| Filter | `Api` Middleware／Filter | 例外、授權、稽核管線是否仍適用新端點 |
| Mapper | `Core` Mapperly `{Name}Mapper` | DTO↔Entity 映射完整、Warning 當 Error |
| UoW | `Infrastructure` `DbSession`／Unit of Work | 寫入路徑有交易邊界 |
| DI | `Api/IoC/`、Web `Program.cs` | Repository／Service／`PgmApiClient`／HttpClient 已註冊 |
| DapperTypeMap | `Infrastructure/Persistence/DapperTypeMapConfig.cs` | PascalCase Entity ↔ SCREAMING_SNAKE 欄位 |

## 2. 命名與目錄約定

| 種類 | 約定 |
|---|---|
| Entity | `Core/Domain/Entities/{Name}.cs`（PascalCase） |
| DTO／Request | `Core/Application/Models/...` |
| Repository | `I{Name}Repository`／`{Name}Repository` |
| Service | `I{Name}Service`／`{Name}Service`（`[ScopedRegistration]`） |
| Mapper | Mapperly：`{Name}Mapper` |
| Controller | `{Name}Controller` → `/api/...` |
| Blazor | `Web/Components/Pages/...` |
| 領域文件 | `domain/{Name}.md` |

### 本專案領域對照

| 領域文件 | SRS | 主要表 |
|---|---|---|
| `domain/Login.md` | Login | `EMP_USER`、`MAP_USER_ROLE`、`DIM_ROLE`、`MAP_ROLE_FUNCTION`、`SET_FUNCTION`、`CHANGE_PASSWORD_HISTORY` |
| `domain/EmpUser.md` | EMPSet | `EMP_USER`、`MAP_USER_ROLE`、`DIM_ROLE`（R） |
| `domain/RoleFunction.md` | RoleFunctionSet | `MAP_ROLE_FUNCTION`、`DIM_ROLE`（R）、`SET_FUNCTION`（R） |
| `domain/ParamSet.md` | ParamSet | `SET_PARAM`、`SET_PARAMITEM`（R） |
| `domain/SetFunction.md` | （IT／Seed） | `SET_FUNCTION`（無 UI） |

## 3. 程式碼品質鐵律

- 禁止 Controller／Repository 內寫商業規則（規則在 Service）
- 使用 Mapperly；Warning 當 Error（若專案已設定）
- 密碼只經 `IPasswordHasher`（BCrypt Verify／Hash）
- 角色功能授權：**全量刪後插** `MAP_ROLE_FUNCTION`（對齊 SRS）
- 帳號角色指派：**先刪後插** `MAP_USER_ROLE`
- 軟刪：`DEL_FLG = 1`（bit）；查詢活動中 `DEL_FLG = 0`
- 例外應由既有全域例外處理機制統一轉譯與記錄；僅在能增加脈絡、補償或轉換已知例外時才於區域捕捉，**禁止**為了記錄而重複 `catch` 後再拋出
- 日誌採結構化欄位，禁止記錄密碼、Token、連線字串、身分證號、完整信用卡號或其他機敏資料
- 最小變更；未經核准不得借機重構、不刪既有 Architecture Tests

## 4. 從舊 DGPM 模型遷移注意

目前程式可能仍殘留 `SysFun`／`MAP_ROLE_RIGHT` 命名。**目標狀態**：

- 表與 Repository → `SET_FUNCTION`、`MAP_ROLE_FUNCTION`
- 選單查詢依角色過濾（SRS Login §顯示功能）
- 參數表 PK＝`(SET_ITEM, SET_ID)`（BMW），不是舊的 `SET_TYPE`

修改時以 SQL／domain 為準，逐步改程式，避免雙軌並存寫入。

## 5. 測試與驗證

- 新 Service／Controller 至少單元測試
- 登入、改密、角色切換選單、角色授權覆寫、ParamSet 軟刪復活：優先補測試
- 未跑測試須在交付格式註明「未驗證」
- 建議指令：

```bat
dotnet build PGM.slnx
dotnet test PGM.slnx
```

## 6. PR／Commit 檢查清單

- [ ] 符合 `AGENT_CONSTITUTION.md` 優先序與鐵律
- [ ] 已確認本次風險等級；高風險變更已有核准的影響分析（`AGENT_ANALYSIS.md`）
- [ ] 最小變更原則（未經核准不得重構）
- [ ] 權限鏈與資安（BCrypt、Authorize、依角色選單）已處理；**不採納** Login SRS 明文密碼 SQL／`DEL_FLG='N'`
- [ ] Template 掛點：Filter、Mapper、UoW、DI、DapperTypeMap 已逐項確認
- [ ] 測試已執行或標註未驗證
- [ ] 已更新 `domain/{Entity}.md`、`SQL/`、`SQL/_SQL_README.md` 或 README（規則、契約或操作方式有變更時）
- [ ] 交付格式八段齐全

## 7. 常用路徑速查

```text
Api/Controllers/
Core/Application/Services/
Core/Domain/Entities/
Infrastructure/Repositories/
Infrastructure/Persistence/DapperTypeMapConfig.cs
Web/Components/Pages/
Web/Services/PgmApiClient.cs
SQL/10_dbo_pgm_tables.sql
SQL/90_dev_seed_admin.sql
domain/
docs/
AGENT_CONSTITUTION.md
AGENT_IMPLEMENT.md
AGENT_ANALYSIS.md
```
