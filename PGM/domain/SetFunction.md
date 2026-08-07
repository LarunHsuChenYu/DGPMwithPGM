# SetFunction 領域規則 — PGM

## 1. 基本說明
- 中文名稱：功能主檔
- 英文名稱：SET_FUNCTION（**取代 SysFun 表名**）
- 主責系統：PGM
- 資料寫入者：IT／DBA／PGM 維護 UI（`FunctionList`；亦可 Seed／SQL）
- 同步方向／頻率：DGPM 選單只讀本表（`SYSTEM_CODE=DGPM` 列）；業務 Fun 以 PGM Seed／SQL 或 FunctionList 建碼
- 主要識別鍵與對照鍵：`FUNCTION_ID`；階層 `PARENT_ID`；系統隔離 `SYSTEM_CODE`（`PGM`｜`DGPM`）
- 資料保留／封存年限：軟刪保留
- 主要來源：BMW `SET_FUNCTION`；擴充欄語意來自原 SysFun；SRS 中為 **R**（IT 維護）

## 2. 狀態機
| 狀態碼 | 狀態名稱 | 可轉移狀態 | 轉移動作 | 備註 |
|--------|----------|------------|----------|------|
| 0 | 未刪 | 1 | 軟刪 | `DEL_FLG=1` |
| Y/N | IS_ENABLED | — | Seed／SQL | 選單另要求 IS_MENU／IS_ENABLED |

目前以 `SQL/90_dev_seed_admin.sql` 或手動 SQL 維護；簡易維護 UI 已落地：`FunctionList`、`RoleMaster`（定案 Q13）。

## 3. 主責欄位清單
**BMW：** FUNCTION_ID、FUNCTION_NAME、FUNCTION_URL、PARENT_NAME、SORT_ID、DEL_FLG、CRT_*、MDF_*  
**擴充（超出 BMW LIST、專案定案）：** PARENT_ID、ACTION_TYPE（M／P／B）、IS_MENU、IS_ENABLED、FUN_DESC、ICON、**SYSTEM_CODE**  

- PK：`FUNCTION_ID`
- 階層真相：`PARENT_ID`；`PARENT_NAME` 供 SRS 顯示／相容
- `SYSTEM_CODE`：`PGM`｜`DGPM`，預設 `PGM`（DDL 見 `SQL/10_*.sql`／`20_dbo_system_code.sql`）
- 欄位主責、可由何系統更新、衝突解決方式：Seed／SQL 或 FunctionList（有權限者）寫入

## 4. 關鍵業務規則
1. 不建 `SysFun` 表；所有功能／選單讀寫 `SET_FUNCTION`
2. 模組列：`ACTION_TYPE='M'`、`PARENT_ID` NULL、`FUNCTION_URL` 可空
3. 葉功能：`ACTION_TYPE='P'`，`PARENT_ID` 指向模組
4. 選單候選：`DEL_FLG=0` AND `IS_MENU='Y'` AND `IS_ENABLED='Y'`，並經 `MAP_ROLE_FUNCTION` 過濾，且 `SYSTEM_CODE` 與登入端一致
5. 僅授權葉 P 時，父模組 M **自動出現**於選單（定案 Q7）
6. `FUNCTION_URL` 長度 100，避免截斷
7. AUTH05（系統報表）維持佔位、不納入聯調驗收（定案 Q12）

## 5. 外部整合
- 涉及系統：DGPM 業務選單（只讀、`SYSTEM_CODE=DGPM`）
- 主從關係與衝突解決規則：PGM 為功能主檔主責
- 介接方式、方向與觸發時機：Login／menus API；契約見 `docs/contracts/auth-consumer-contract.md`
- 契約版本、必要欄位與相容性期間：見 `docs/contracts/pgm-dgpm-decisions.md`
- 冪等鍵／去重規則：`FUNCTION_ID` PK；Seed 宜 idempotent
- 失敗處理、重試上限、Dead Letter Queue 與人工補償：手動 SQL 修復
- 對帳方式、對帳頻率與責任人：Seed 後核對選單與 `MAP_ROLE_FUNCTION`

## 6. 權限矩陣
| 角色 | 查詢（執行時選單） | 維護 UI | 備註 |
|------|-------------------|--------|------|
| 已授權角色 | 僅自己的功能（同 SYSTEM_CODE） | 依功能授權 | |
| IT／DBA／PGMAdmin | SQL／Seed 或 FunctionList／RoleMaster | Y（有 AUTH06／角色主檔權） | |

## 7. 資料品質與稽核
- Seed 必須同時維護 PARENT_ID 與 PARENT_NAME 一致
- 禁止再寫入舊 `MAP_ROLE_RIGHT` 鏈
- 外部匯入是否需 Staging？否（僅 SQL）

## 8. 驗收與回復
- 必要測試：Seed 後選單階層、依角色過濾、URL 長度
- 上線前置：執行 `SQL/90_dev_seed_admin.sql`（或同等）並核對
- 回復：還原 `SET_FUNCTION`／相關 `MAP_ROLE_FUNCTION` 備份（高風險，須授權）

## 9. 開放問題與決策紀錄

| 日期 | 問題／決策 | 決策者 | 影響範圍 | 追蹤項目 |
|---|---|---|---|---|
| 2026-08-05 | `SYSTEM_CODE`＝`PGM`／`DGPM`；業務 Fun Seed；父層自動顯示；AUTH05 不納聯調；維護 UI **已落地** | 已確認 | SET_FUNCTION／DGPM | `pgm-dgpm-decisions.md` |
| | 是否允許 `ACTION_TYPE='B'`（按鈕）；目前無來源則可不使用 | 待定 | SetFunction／授權 UI | |
| 2026-08-03 | 功能代碼改採系統代碼 `AUTH01`～`AUTH05`（與 ParamSet／RoleFunctionSet SRS 畫面一致）；舊英文 ID 軟刪 | 已確認 | SET_FUNCTION／MAP_ROLE_FUNCTION／Seed | 重跑 `90_dev_seed_admin.sql` |
