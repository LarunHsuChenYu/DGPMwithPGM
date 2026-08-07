# domain/ — PGM 領域規則目錄

每個領域一份 `{Name}.md`，供 Agent 在實作前對齊業務規則。  
新建領域請複製 [`_template.md`](_template.md)。規格原件仍放在 `docs/`，勿搬入本目錄。

| 文件 | SRS／用途 |
|---|---|
| [_template.md](_template.md) | 新建領域模板 |
| [Login.md](Login.md) | 登入、角色切換、強制改密 |
| [EmpUser.md](EmpUser.md) | 使用者帳號維護 |
| [RoleFunction.md](RoleFunction.md) | 角色權限設定 |
| [ParamSet.md](ParamSet.md) | 系統代碼維護 |
| [SetFunction.md](SetFunction.md) | 功能主檔（Seed／SQL＋FunctionList UI） |

### Phase 3／DGPM 契約（必讀於聯調或改 JWT／選單時）

| 文件 | 用途 |
|---|---|
| [`docs/contracts/pgm-dgpm-decisions.md`](../docs/contracts/pgm-dgpm-decisions.md) | 前提與 Q1～15 定案、角色、驗收、缺口 |
| [`docs/contracts/auth-consumer-contract.md`](../docs/contracts/auth-consumer-contract.md) | JWT Claim／Login／錯誤碼（兩專案共用） |
| [`docs/contracts/data-scope-emp-org.md`](../docs/contracts/data-scope-emp-org.md) | EMP_ORG／Dealer 範圍定義（本 Phase 不實作） |

變更領域規則時：同步考慮 `SQL/`、Api Contract；依 [`AGENT_CONSTITUTION.md`](../AGENT_CONSTITUTION.md) §7 判定風險等級，高風險須先走 [`AGENT_ANALYSIS.md`](../AGENT_ANALYSIS.md)。
