# SQL Schema 腳本（PGM）

依 [`docs/BMWv20260720.md`](../docs/BMWv20260720.md)（LIST）與四份 PGM SRS 建表。  
**功能主檔表名為 `SET_FUNCTION`（不以 `SysFun` 建表）**；欄位含 BMW 基底＋SysFun 階層／選單擴充。

## 不實作

| 表 | 原因 |
|---|---|
| `DIM_RIGHT` | LIST 未收錄；SRS 未用 |
| `MAP_ROLE_RUNCTION` | LIST 未收錄；疑為拼字錯誤 |
| `MAP_ROLE_RIGHT`／`MAP_RIGHT_FUNCTION` | 舊 DGPM 模型；SRS 改為 `MAP_ROLE_FUNCTION` |

## 執行順序

| 順序 | 檔案 | 內容 |
|---|---|---|
| 1 | [`10_dbo_pgm_tables.sql`](10_dbo_pgm_tables.sql) | LIST 表 DDL（含 `SET_FUNCTION`、`SYSTEM_CODE`） |
| 2 | [`20_dbo_system_code.sql`](20_dbo_system_code.sql) | 既有庫補 `SYSTEM_CODE`（可與 10 重跑） |
| 3 | [`90_dev_seed_admin.sql`](90_dev_seed_admin.sql) | PGMAdmin／DGPM* 角色＋AUTH*／DGPM 業務選單＋測帳 AshtonHsu／Admin |

```bat
sqlcmd -S <server> -d PGM_DEV -f 65001 -I -i SQL\10_dbo_pgm_tables.sql
sqlcmd -S <server> -d PGM_DEV -f 65001 -I -i SQL\20_dbo_system_code.sql
sqlcmd -S <server> -d PGM_DEV -f 65001 -I -i SQL\90_dev_seed_admin.sql
```

> **必選對資料庫**：`10`／`20`／`90` 開頭皆 `USE [PGM_DEV]`。SSMS 若只開在 `master` 跑舊版 `20`（無 USE），欄位不會加到 `PGM_DEV`，接著跑 `90` 會出現「無效的資料行名稱 SYSTEM_CODE」（編譯期 207）。請先在 `PGM_DEV` 重跑更新後的 `20`，再跑 `90`。

### SSMS／編碼注意

- **資料庫必須選對**：查詢視窗工具列下拉選 `PGM_DEV`（或實際目標庫）。Object Explorer 展開哪個庫**不會**自動切換查詢視窗；在 `master` 執行會出現大量「物件名稱／資料行名稱無效」。
- 腳本為 **UTF-8 BOM**。SSMS 請用「開啟時指定 UTF-8」，勿以 Big5／系統預設開啟，否則註解錯位時可能把 `SET_FUNCTION` 當成獨立語句 → 訊息 2812「找不到預存程序」。
- `SET` 為 T-SQL 保留字；腳本內物件參考寫成 `dbo.[SET_FUNCTION]`／`dbo.[SET_PARAMITEM]`／`dbo.[SET_PARAM]`（表名不變，僅 delimiting）。
- `90_dev_seed_admin.sql` 開頭會檢查核心表是否存在；失敗時訊息會印出**目前資料庫名稱**。

## 權限鏈（SRS Login／RoleFunctionSet）

```text
EMP_USER
  → MAP_USER_ROLE → DIM_ROLE
  → MAP_ROLE_FUNCTION → SET_FUNCTION
```

登入選單語意（SRS）：

```sql
SELECT A.PARENT_NAME, A.FUNCTION_NAME, A.FUNCTION_URL
FROM SET_FUNCTION A
JOIN MAP_ROLE_FUNCTION B ON A.FUNCTION_ID = B.FUNCTION_ID
WHERE B.ROLE_ID = @ROLE_ID
  AND A.DEL_FLG = 0
ORDER BY A.PARENT_NAME, A.SORT_ID;
```

階層／啟用另可用擴充欄：`PARENT_ID`、`ACTION_TYPE`、`IS_MENU`、`IS_ENABLED`。

## SET_FUNCTION 欄位摘要

| 來源 | 欄位 |
|---|---|
| BMW | `FUNCTION_ID`, `FUNCTION_NAME`, `FUNCTION_URL`, `PARENT_NAME`, `SORT_ID`, `DEL_FLG`, `CRT_*`, `MDF_*` |
| SysFun 擴充 | `PARENT_ID`, `ACTION_TYPE` (M/P/B), `IS_MENU`, `IS_ENABLED`, `FUN_DESC`, `ICON` |

開發種子功能代碼（`90_dev_seed_admin.sql`）：
- PGM：`AUTH01`～`AUTH08`（帳號／角色／改密／代碼／報表／功能／角色主檔／登入紀錄）
- DGPM：業務模組葉（`RoleKPIList` 父層＝`KPIIndicator`）；`PgmAuthLink`／`Permission` 軟刪、不授權給 DGPM*
- **跨系統**：`Admin` 與 `AshtonHsu` 皆掛 `PGMAdmin`＋`DGPMAdmin`（系統權限同等；AshtonHsu 另含 Uploader／Reviewer）；舊 `ADMIN` 軟刪

## 其他表

| 表 | 用途 |
|---|---|
| `EMP_USER` | 帳號（BCrypt） |
| `DIM_ROLE`／`MAP_USER_ROLE` | 角色與指派 |
| `MAP_ROLE_FUNCTION` | 角色×功能 |
| `SET_PARAMITEM`／`SET_PARAM` | 系統代碼 |
| `CHANGE_PASSWORD_HISTORY` | 改密歷程 |
| `AUTHENTICATION_LOG` | 登入／登出軌跡（非 BMW LIST；Login 寫入） |
| `EMP_ORG` | 組織（僅 DDL 預留） |
