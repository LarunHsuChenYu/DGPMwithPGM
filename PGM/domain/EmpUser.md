# EmpUser 領域規則 — PGM

## 1. 基本說明
- 中文名稱：使用者帳號維護
- 英文名稱：EMPSet／Accounts
- 主責系統：PGM
- 資料寫入者：PGM Api（EMPSet）
- 同步方向／頻率：無（不與 QMS 共用）
- 主要識別鍵與對照鍵：`EMP_USER.USER_ID`；角色對照 `MAP_USER_ROLE`
- 資料保留／封存年限：軟刪保留列；未定封存政策前全留
- 主要來源：`docs/PGM_Qlik_EMPSet20260719 .docx`；BMW `EMP_USER`／`MAP_USER_ROLE`／`DIM_ROLE`

## 2. 狀態機
| 狀態碼 | 狀態名稱 | 可轉移狀態 | 轉移動作 | 轉移條件 | 備註 |
|--------|----------|------------|----------|----------|------|
| 0 | 活動 | 1 | 軟刪 | 確認對話 | `DEL_FLG=1`，並刪除該帳 `MAP_USER_ROLE` |
| 1 | 停用 | 0 | （若允許復用） | 產品未定 | SRS 刪除為軟刪 |

## 3. 主責欄位清單
- `EMP_USER`：USER_ID（PK，建立後不可改）、USER_NAME、PASSWORD、EMAIL、TELEPHONE、DPT_CODE、DEL_FLG、稽核欄（**無**舊 DGPM 的 `TIT_NAME`／`FACTORY_NO`）
- `MAP_USER_ROLE`：USER_ID＋ROLE_ID；儲存時**先刪後插**
- `DIM_ROLE`：畫面只讀下拉（IT 維護角色主檔；BMW **無** `ROLE_TYPE`）
- 欄位主責、可由何系統更新、衝突解決方式：PGM 唯一寫入者

## 4. 關鍵業務規則
1. 新增／編輯：帳號、姓名、Email、電話、角色（多選）必填（依 SRS 畫面）
2. 新增：INSERT `EMP_USER`（初始密碼策略依實作／種子）；角色刪後插
3. 編輯：USER_ID 鎖定；UPDATE 基本欄；角色刪後插
4. 刪除：確認後 `DEL_FLG=1`，並 `DELETE MAP_USER_ROLE WHERE USER_ID=…`
5. 列表：活動帳號＋角色名稱聚合顯示（SRS 範例 SQL）

## 5. 外部整合
- 涉及 ERP／SCM／MES／BI／EDI／SSRS／其他：本階段無
- 主從關係與衝突解決規則：PGM 主責帳號與角色指派
- 介接方式、方向與觸發時機：無
- 契約版本、必要欄位與相容性期間：N/A
- 冪等鍵／去重規則：USER_ID 唯一；角色指派先刪後插
- 失敗處理、重試上限、Dead Letter Queue 與人工補償：交易失敗整筆回滾
- 對帳方式、對帳頻率與責任人：N/A

## 6. 權限矩陣
| 角色 | 查詢 | 新增 | 修改 | 刪除 | 備註 |
|------|------|------|------|------|------|
| 系統管理（有本功能授權） | Y | Y | Y | Y | 功能代碼 `AUTH01` |
| 其他 | 依 `MAP_ROLE_FUNCTION` | | | | |

## 7. 資料品質與稽核
- USER_ID 長度 ≤10（BMW）
- PASSWORD 必為加密存儲
- 外部匯入是否需 Staging？否
- 重送／重複匯入防護：USER_ID PK
- 資料修復：軟刪誤操作可由授權人員復活（若產品允許）並重指角色

## 8. 驗收與回復
- 必要測試：新增／編輯／軟刪、角色多選刪後插、無權限拒絕、USER_ID 不可改
- 上線前置：DIM_ROLE／功能授權已 Seed → Api → Web
- 回復：還原部署；誤刪帳號以 `DEL_FLG` 復活＋重補 `MAP_USER_ROLE`

## 9. 開放問題與決策紀錄

| 日期 | 問題／決策 | 決策者 | 影響範圍 | 追蹤項目 |
|---|---|---|---|---|
| 2026-08-03 | 新增帳號預設密碼固定 `0000`；首次登入以 BCrypt Verify 判定 FORCE_PWD，改密後寫 `CHANGE_PASSWORD_HISTORY` | 產品／實作 | EMPSet／Login | |
| | Email／電話 BMW 可 NULL、SRS 標必填 → 以 SRS 畫面為準 | 待確認實作註記 | EMPSet | |
