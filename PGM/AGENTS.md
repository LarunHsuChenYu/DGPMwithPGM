# AGENTS.md — PGM Agent 群入口

本專案的 AI／開發代理**一律**依下列文件群運作。衝突時以 [`AGENT_CONSTITUTION.md`](AGENT_CONSTITUTION.md) 為最高專案憲法（僅次於使用者當前指示與已核准 SA 規格）。

## 文件地圖

| 文件 | 角色 | 何時必讀 |
|---|---|---|
| [`AGENT_CONSTITUTION.md`](AGENT_CONSTITUTION.md) | 憲法：優先序、分層、資安、DB、風險分級、交付格式 | **每次**實作／重構／審查 |
| [`AGENT_IMPLEMENT.md`](AGENT_IMPLEMENT.md) | 實作 SOP：十步驟、命名、測試、PR 清單 | 新增／修改功能時 |
| [`AGENT_ANALYSIS.md`](AGENT_ANALYSIS.md) | SA／高風險分析：影響矩陣、回復、Open Questions | SA 模式或高風險變更停等前 |
| [`domain/_Domain_README.md`](domain/_Domain_README.md) | 領域規則：Login／帳號／角色權限／參數／功能主檔 | 動到該領域前 |
| [`SQL/_SQL_README.md`](SQL/_SQL_README.md) | 表清單與權限鏈（BMW LIST＋SRS） | 任何 DB／Repository 變更 |
| [`README.md`](README.md) | 技術選型、端點、專案結構 | 架構／API 契約變更 |
| [`docs/contracts/`](docs/contracts/) | Phase 3 Auth 契約、資料範圍定義、UAT 清單 | DGPM 外連／契約異動 |
| [`docs/`](docs/) | SA 規格與資料典（四份 SRS＋BMW） | 規格對照、欄位定案 |

## Agent 角色（建議切換）

| 角色 | 職責 | 必讀 |
|---|---|---|
| **憲法守護（Constitution）** | 拒絕違規分層／資安捷徑；依風險分級要求停等 | `AGENT_CONSTITUTION.md` §2～§7 |
| **實作（PG）** | 依 SOP 最小變更實作 | `AGENT_CONSTITUTION.md`＋`AGENT_IMPLEMENT.md`＋對應 `domain/*.md` |
| **分析（SA／高風險）** | 產出影響分析、回復計畫與待決策；停等確認 | `AGENT_CONSTITUTION.md` §7＋`AGENT_ANALYSIS.md`＋`docs/` |
| **領域（Domain）** | 釐清狀態、權限、表欄位；更新 domain 文件 | `domain/{Entity}.md`＋`docs/` SRS；新建用 `_template.md` |
| **SA／規格對照** | 對齊 BMW／SRS；標開放問題 | `docs/BMWv20260720.md`、四份 `PGM_Qlik_*.docx` |
| **DB** | 只改 `SQL/`；權限鏈必須 `MAP_ROLE_FUNCTION`→`SET_FUNCTION` | `SQL/_SQL_README.md`、`AGENT_CONSTITUTION.md` §5 |

## 本專案範圍（與計畫一致）

- **做**：獨立權限平台 — Login、EMPSet、RoleFunctionSet、ParamSet
- **表**：BMW LIST；功能主檔＝**`SET_FUNCTION`**（含 SysFun 擴充欄＝**超出 BMW LIST、專案定案**；**不建 SysFun 表**）
- **密碼**：BMW `DEL_FLG` bit＋BCrypt Verify；**不採納** Login SRS 明文比對範例；與 QMS 加密一致未定案前各自獨立
- **Phase 3**：帳號／角色／參數／登入主責＝**唯 PGM**；DGPM（`AuthMode=PGM`）外連本平台。契約見 [`docs/contracts/auth-consumer-contract.md`](docs/contracts/auth-consumer-contract.md)；資料範圍定義見 [`docs/contracts/data-scope-emp-org.md`](docs/contracts/data-scope-emp-org.md)。
- **不做**：`DIM_RIGHT`／`MAP_ROLE_RUNCTION`；QMS 共用帳；本階段不實作 Dealer 資料範圍過濾。
- **路徑**：領域規則＝根目錄 `domain/`；規格原件＝`docs/`（勿混用）

## Cursor Rules

專案規則位於 [`.cursor/rules/`](.cursor/rules/)，會自動帶入本 Agent 群約束。
