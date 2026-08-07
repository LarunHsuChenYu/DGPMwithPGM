# RoleFunction 領域規則 — PGM

## 1. 基本說明
- 中文名稱：角色權限設定
- 英文名稱：RoleFunctionSet
- 主責系統：PGM
- 資料寫入者：PGM Api（僅寫 `MAP_ROLE_FUNCTION`；`DIM_ROLE`／`SET_FUNCTION` 畫面只讀）
- 同步方向／頻率：無
- 主要識別鍵與對照鍵：`ROLE_ID`＋`FUNCTION_ID`
- 資料保留／封存年限：對照表以現行授權為準；覆寫即刪舊
- 主要來源：`docs/PGM_Qlik_RoleFunctionSet20260720.docx`；BMW `MAP_ROLE_FUNCTION`、`DIM_ROLE`、`SET_FUNCTION`

## 2. 狀態機
無獨立狀態機。授權為**全量覆寫**：對某 `ROLE_ID` 刪光後再插入勾選功能。允許「全不勾選」＝該角色無權限。

## 3. 主責欄位清單
- `DIM_ROLE`：ROLE_ID、ROLE_NAME、**SYSTEM_CODE**、DEL_FLG（畫面 R；IT／Seed；簡易 UI 另排）
- `SET_FUNCTION`：FUNCTION_ID、FUNCTION_NAME、**SYSTEM_CODE**…（畫面 R；Seed／SQL）
- `MAP_ROLE_FUNCTION`：ROLE_ID＋FUNCTION_ID、CRT_DATE、CRT_USER（**不加** SYSTEM_CODE 欄）
- 欄位主責、可由何系統更新、衝突解決方式：授權對照僅 PGM 寫入；全量覆寫解決衝突

## 4. 關鍵業務規則
1. 角色下拉：`DIM_ROLE` 且 `DEL_FLG=0`（維護畫面可再依 `SYSTEM_CODE` 過濾）
2. Grid：全部未刪功能 LEFT JOIN 該角色 `MAP_ROLE_FUNCTION`；有對應則 checkbox 勾選；功能應與角色同 `SYSTEM_CODE`
3. 確認：必須已選角色；`DELETE MAP_ROLE_FUNCTION WHERE ROLE_ID=@ROLE_ID` 後，對勾選項 `INSERT`
4. **拒絕跨系統勾選**（角色 `SYSTEM_CODE` ≠ 功能 `SYSTEM_CODE`）；隔離靠兩邊欄位，MAP 不加欄（定案 Q11）
5. 授權以葉功能 P 為主；父模組 M 由選單組裝自動帶出（定案 Q7）
6. **不使用** `MAP_ROLE_RIGHT`／`MAP_RIGHT_FUNCTION`／`DIM_RIGHT`；不做舊 RIGHT 轉換（定案 Q5）
7. 執行時選單／權限檢查必須讀此對照（與 Login 領域一致）
8. 業務角色（`DGPMAdmin`／`Uploader`／`Reviewer`）預設不授權 AUTH01～04（定案 Q4）

## 5. 外部整合
- 涉及系統：DGPM 消費選單／JWT（不直寫 MAP）
- 主從關係與衝突解決規則：PGM 為授權真相
- 介接方式、方向與觸發時機：見 `docs/contracts/auth-consumer-contract.md`
- 契約版本、必要欄位與相容性期間：見 `docs/contracts/pgm-dgpm-decisions.md`
- 冪等鍵／去重規則：全量刪後插（同一 ROLE_ID 一次儲存＝最終真相）
- 失敗處理、重試上限、Dead Letter Queue 與人工補償：單一交易；失敗整筆回滾
- 對帳方式、對帳頻率與責任人：儲存後查 `MAP_ROLE_FUNCTION` 筆數／清單

## 6. 權限矩陣
| 角色 | 查詢 | 儲存授權 | 備註 |
|------|------|----------|------|
| 有 AUTH02（角色權限設定）功能者 | Y | Y | |
| 其他 | N | N | |

## 7. 資料品質與稽核
- CRT_USER／CRT_DATE 於每次 INSERT 寫入目前登入者
- 建議僅對 `IS_ENABLED='Y'` 且 `DEL_FLG=0` 的功能列供勾選
- 重送／重複儲存：以最後一次全量覆寫為準

## 8. 驗收與回復
- 必要測試：全選／全不選、單角色覆寫、選單立即反映、無權限拒絕儲存
- 上線前置：`SET_FUNCTION`／`DIM_ROLE` Seed 完備
- 回復：還原該 ROLE_ID 先前 `MAP_ROLE_FUNCTION` 備份（儲存前宜可匯出）

## 9. 開放問題與決策紀錄

| 日期 | 問題／決策 | 決策者 | 影響範圍 | 追蹤項目 |
|---|---|---|---|---|
| 2026-08-05 | 僅授權葉 P；父 M 自動顯示；MAP 不加 SYSTEM_CODE；跨系統勾選應拒絕；不做舊 RIGHT 轉換 | 已確認 | RoleFunction／Login／DGPM | `pgm-dgpm-decisions.md` Q5／Q7／Q11 |
