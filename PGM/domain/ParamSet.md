# ParamSet 領域規則 — PGM

## 1. 基本說明
- 中文名稱：系統代碼維護
- 英文名稱：ParamSet
- 主責系統：PGM
- 資料寫入者：PGM Api（僅 `SET_PARAM`；`SET_PARAMITEM` 畫面只讀）
- 同步方向／頻率：無
- 主要識別鍵與對照鍵：`(SET_ITEM, SET_ID)`
- 資料保留／封存年限：軟刪保留；未定封存前全留
- 主要來源：`docs/PGM_Qlik_ParamSet20260719 .docx`；BMW `SET_PARAM`／`SET_PARAMITEM`

## 2. 狀態機
| 狀態碼 | 狀態名稱 | 可轉移狀態 | 轉移動作 | 轉移條件 | 備註 |
|--------|----------|------------|----------|----------|------|
| 0 | 有效 | 1 | 軟刪 | 確認 | `DEL_FLG=1` |
| 1 | 已刪 | 0 | 復活 | 新增時發現同鍵且已刪 | SRS：恢復並更新值／排序 |

## 3. 主責欄位清單
- `SET_PARAMITEM`：SET_ITEM（PK）、SET_ITEM_NAME、MEMO、DEL_FLG、稽核（畫面 **只讀**）
- `SET_PARAM`：SET_ITEM＋SET_ID（複合 PK）、SET_VALUE、SORT_ORDER、MEMO、DEL_FLG、稽核
- 欄位主責、可由何系統更新、衝突解決方式：PGM 寫 `SET_PARAM`；類別主檔 IT／SQL

## 4. 關鍵業務規則
1. 類別下拉：`SET_PARAMITEM` 且 `DEL_FLG=0`
2. Grid：依選中 SET_ITEM join 明細，雙方 `DEL_FLG=0`，`ORDER BY SORT_ORDER`
3. 新增：檢核必填；若同 SET_ITEM＋SET_ID 已存在且 `DEL_FLG=1` → **復活**；若 `DEL_FLG=0` → 重複錯誤；否則 INSERT
4. 編輯：SET_ID 不可改；更新 SET_VALUE、SORT_ORDER
5. 刪除：軟刪 `DEL_FLG=1`
6. 新增時 SORT_ORDER 預設 `MAX+1`（可改）

## 5. 外部整合
- 涉及 ERP／SCM／MES／BI／EDI／SSRS／其他：本階段無
- 主從關係與衝突解決規則：PGM 主責代碼明細
- 介接方式、方向與觸發時機：無
- 契約版本、必要欄位與相容性期間：N/A
- 冪等鍵／去重規則：複合 PK；已刪列復活而非重複 INSERT
- 失敗處理、重試上限、Dead Letter Queue 與人工補償：交易回滾
- 對帳方式、對帳頻率與責任人：N/A

## 6. 權限矩陣
| 角色 | 查詢 | 新增 | 修改 | 刪除 | 備註 |
|------|------|------|------|------|------|
| 有 ParamSet 功能者 | Y | Y | Y | Y | SET_PARAMITEM 不開放維護 UI |

## 7. 資料品質與稽核
- SET_ID ≤20、SET_VALUE ≤50（BMW）
- CRT_USER／MDF_USER 寫登入者
- 外部匯入是否需 Staging？否
- 重送／重複：同鍵活動中拒重；已刪則復活

## 8. 驗收與回復
- 必要測試：新增／編輯／軟刪／復活、SORT_ORDER 預設、類別只讀
- 上線前置：`SET_PARAMITEM` Seed
- 回復：軟刪誤操作可復活；錯誤值還原上次 SET_VALUE

## 9. 開放問題與決策紀錄

| 日期 | 問題／決策 | 決策者 | 影響範圍 | 追蹤項目 |
|---|---|---|---|---|
| | 舊程式若仍用 `SET_TYPE`，應遷移為 BMW `SET_ID` | 待實作 | ParamSet／Repository | Phase 1 |
