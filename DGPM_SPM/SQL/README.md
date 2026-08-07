# SQL Schema 腳本

本資料夾存放 DGPM_SPM 的 SQL Server DDL 腳本，按 schema/domain 分檔。**其中 `15_dbo_sysfun.sql` 的 `dbo.SysFun` 為 SA 提供的正式表腳本；其餘 schema／物件多為本專案端依既有需求虛擬建立的命名相容定義或 SDS（系統設計規格書）到位前的 provisional draft，不得視為正式規格。**

ERD 與資料模型說明見根目錄 [`README.md`](../README.md) 的「資料庫設計（SDS 前暫定）」章節。

> **不是連正式 QMS**：表名雖相容 QMS（`EMP_USER` 等），本機開發是連**本專案自己的資料庫**。`90_dev_seed_admin.sql` 寫入的是 DGPM 開發種子帳號／選單，**不會、也不應**連回正式 QMS。

## 檔案清單與執行順序

依檔名數字前綴由小到大依序執行：

| 順序 | 檔案 | Schema | 內容 | 性質 |
|---|---|---|---|---|
| 1 | `00_create_schemas.sql` | `org` / `cfg` / `kpi` | 建立業務 schema | draft |
| 2 | `10_dbo_qms_compat.sql` | `dbo` | 命名相容表（EMP_USER、DIM_ROLE、MAP_USER_ROLE、SET_FUNCTION、MAP_RIGHT_FUNCTION、MAP_ROLE_RIGHT、SET_PARAM、AUTHENTICATION_LOG） | **命名相容 DDL**（欄位反推；本機建表用） |
| 3 | `15_dbo_sysfun.sql` | `dbo` | **正式表 `SysFun`**（TableList）＋業務模組種子；PGM 主責選單項軟刪 | **SA 提供的正式規格 DDL＋種子**（可重複執行） |
| 4 | `20_org_master_data.sql` | `org` | 區域組織（REGION）、經銷商（DEALER） | draft |
| 5 | `30_cfg_system_parameters.sql` | `cfg` | 匯率參數（EXCHANGE_RATE） | draft |
| 6 | `40_kpi_dealer_kpi.sql` | `kpi` | KPI 指標、匯入批次、KPI 數據、異動紀錄、資料權限 | draft |
| 7 | `90_dev_seed_admin.sql` | `dbo` | **DGPM 開發種子**：EMP_USER（KPI 對照用）／ADMIN；**非** Local Auth 登入源 | **僅本機／開發**（可重複執行） |

所有腳本皆為 idempotent：schema 用 `SCHEMA_ID` 檢查、資料表用 `OBJECT_ID` 檢查；開發種子用 MERGE／存在則更新，重複執行不會破壞既有業務資料。

```
sqlcmd -S <server> -d <database> -f 65001 -I -i 00_create_schemas.sql
sqlcmd -S <server> -d <database> -f 65001 -I -i 10_dbo_qms_compat.sql
sqlcmd -S <server> -d <database> -f 65001 -I -i 15_dbo_sysfun.sql
sqlcmd -S <server> -d <database> -f 65001 -I -i 20_org_master_data.sql
sqlcmd -S <server> -d <database> -f 65001 -I -i 30_cfg_system_parameters.sql
sqlcmd -S <server> -d <database> -f 65001 -I -i 40_kpi_dealer_kpi.sql
sqlcmd -S <server> -d <database> -f 65001 -I -i 90_dev_seed_admin.sql
```

> 連線資訊由執行者自備（User Secrets／環境變數），本資料夾不存放任何 connection string。開發帳密說明見 [`docs/安裝文件.md`](../docs/安裝文件.md)；`90_dev_seed_admin.sql` 內僅存 BCrypt hash。

## 開發種子（`90_dev_seed_admin.sql`）

- **用途**：種子 `EMP_USER` 等供 KPI 資料權限等業務功能以 `USER_ID` 對照。**不再用於 Local Auth 登入**（登入請走 PGM）。
- **前置**：至少已執行 `10_dbo_qms_compat.sql` 與 `15_dbo_sysfun.sql`（其餘 `00`～`40` 依完整本機環境需要執行）。
- **內容**：建立／更新 `ADMIN` 角色、使用者 `AshtonHsu`、對應 `MAP_USER_ROLE`；並將 `SysFun`（`Del_YN = N`）授權給 `ADMIN`（`RIGHT_ID = ROLE_ID`）。若尚未建 `SysFun`，後備改綁 `SET_FUNCTION`。
- **選單資料**：Web／API 側邊選單改讀 `dbo.SysFun`（`Del_YN=N`、`Is_Menu=Y`、`Is_Enabled=Y`），不再依賴 `SET_FUNCTION`。
- **非正式 QMS**：此種子屬於 DGPM_SPM 專案，請勿在正式／共用 QMS 資料庫執行。

## 既有 naming-compat 與 provisional draft 的邊界

- **`dbo.SysFun` 例外**：`15_dbo_sysfun.sql` 為 SA 提供的正式表腳本，可作為目前 `dbo.SysFun` 的依據。
- **命名相容（其餘 `dbo` 物件）**：`10_dbo_qms_compat.sql` 內各表的**命名與欄位形狀**是為了相容既有系統與沿用 Repository SQL 而建立的最小定義。本機／全新環境可先用這些腳本建表獨立開發，**不必連正式 QMS**；但因缺少正式來源 DDL，多數欄位仍屬反推或虛擬建立，後續需依 SDS 或實際來源校正。
- **provisional draft（`org` / `cfg` / `kpi`）**：依 sitemap 功能推導的暫定設計，支撐後續頁面開發；SDS 定稿後以 SDS 為準調整。

## Rollback 與變更原則

- **不提供通用破壞性 drop script**。除已移除的不再使用清理檔外，本資料夾不維護 `DROP TABLE` / `DROP SCHEMA` 類腳本，避免誤刪資料；如需移除物件，由 DBA 針對個案評估後手動執行。
- **不回頭改已執行過的腳本語意**。腳本一旦在共用環境執行過，後續結構調整應以**新增遞增腳本**（例如 `41_kpi_xxx_alter.sql`）處理，維持檔名前綴排序即為執行順序。
- 新增欄位優先使用 `NULL` 或帶 `DEFAULT`，避免鎖表與破壞既有資料。
- 涉及 `dbo` 命名相容表的結構變更需謹慎評估；若對接真實既有 QMS DB，必須先與對方負責人確認，不得單方面修改對方庫。

## SDS 到位後的調整方式

1. 逐表比對 SDS 與現行 draft 的差異（欄位、型別、約束、命名）。
2. **尚未在任何共用環境執行過的腳本**：直接修訂原檔使其符合 SDS。
3. **已在共用環境執行過的腳本**：以遞增 ALTER 腳本補差異，原檔保留不動。
4. 同步更新根 `README.md` 的 ERD 章節，並移除對應表的 draft 標註。
5. 對應調整 `Core/Domain/Entities` 與 `Infrastructure/Repositories`（含 `DapperTypeMapConfig`）。

## 設計慣例（draft 階段）

- 資料表與欄位名用 SCREAMING_SNAKE_CASE，對應 Domain Entity 的 PascalCase（由 `DapperTypeMapConfig` 集中處理）。
- 每張業務表帶 audit 欄位（`CRT_DATE` / `CRT_USER` / `MDF_DATE` / `MDF_USER`），與既有 QMS 慣例一致。
- 主檔用 `STATUS CHAR(1)`（`A`=啟用 / `I`=停用）表達啟停，而非物理刪除；既有 QMS 表沿用其 `DEL_FLG`。
- 新 schema 內部建實體 FK；跨到 `dbo` 既有 QMS 表（如 `USER_ID`）只做邏輯關聯、不建實體 FK，避免與非本專案所有的表耦合。
