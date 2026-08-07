# 資料範圍與 EMP_ORG（Phase 3 定義文件）

> 本文件僅定義規則與缺口；**本 Phase 不實作** Dealer／KPI 上傳範圍過濾 SQL 或 UI。  
> 主登入、角色、功能授權仍由 **PGM** 控管。

## 1. 目標問題

| 問題 | 說明 |
|---|---|
| 誰可看哪些 Dealer？ | 業務查詢／報表的資料列範圍 |
| 誰可上傳 KPI Excel？ | 功能授權（`SET_FUNCTION`）＋可能再限縮可上傳的經銷商／期間 |
| `EMP_ORG` 是否足夠？ | PGM 預留組織表是否能表達上述範圍 |

## 2. PGM `EMP_ORG` 現況

- 表：`dbo.EMP_ORG`（BMW；DDL 預留）。
- 欄位概要：`DPT_CODE`、`DPT_NO`、`DPTCD_NAME`、`CODE_BOSS`、`DPT_LEVE`、`UPPER_DPT`、稽核欄。
- `EMP_USER.DPT_CODE` 可關聯組織（允許 NULL）。
- **無**經銷商（Dealer）維度、無「使用者↔Dealer」對照、無上傳範圍列。

### 評估結論

| 需求 | `EMP_ORG` 是否足夠 |
|---|---|
| 部門／組織層級（若未來用人資組織當資料範圍） | 部分可以（需補維護與對應規則） |
| Dealer 可見範圍 | **不足** |
| KPI 上傳範圍（Dealer／期間／指標） | **不足** |
| 與角色功能授權（誰可進「KPI 匯入」頁） | 不由 `EMP_ORG` 負責；走 `MAP_ROLE_FUNCTION` |

**結論：** 登入與「能不能進功能」歸 PGM；「能看／上傳哪些业务資料」建議由 **DGPM 業務資料範圍表**承接，不擴充扭曲 `EMP_ORG` 成經銷商矩陣。

## 3. DGPM 業務資料範圍表（草案，未實作）

建議後續另案，示例（非正式 DDL）：

| 概念表 | 用途 | 主鍵構想 |
|---|---|---|
| `USER_DEALER_SCOPE` | 使用者可讀／可上傳之 Dealer | `(USER_ID, DEALER_ID, SCOPE_TYPE)`；`SCOPE_TYPE`=Read／Upload |
| 或掛在既有 KPI 權限表 | 若已有 `RoleKPI`／類似表可演進 | 以現況為準另開 SA |

約束：

- `USER_ID` 必須存在於 **PGM `EMP_USER`**（外連模式下不以 DGPM 本地帳為準）。
- 功能能否開啟仍看 PGM 選單／權限；範圍表只縮小資料列。
- 帳號停用後：下次登入失敗；既有 Token 短效過期；資料 API 應再以 `uid` 查範圍（另案實作）。

## 4. 與角色（Seed）的關係

| ROLE_ID | 功能取向 | 資料範圍（另案） |
|---|---|---|
| PGMAdmin | PGM 管理功能 | 不涉及 Dealer |
| DGPMAdmin | DGPM 業務管理 | 可視為較寬或全 Dealer（待定） |
| DGPMUploader | KPI 上傳／匯入／預覽／紀錄 | Upload 範圍必填（另案） |
| DGPMReviewer | KPI 覆核／解鎖 | Read／Review 範圍（另案） |

## 5. Phase 3 邊界

- **做：** 登入外連、角色、功能選單、`systemCode`、契約。  
- **不做：** 依 Dealer 過濾查詢、上傳前範圍檢核 UI、`EMP_ORG` 維護畫面。  
- 聯調時 KPI 模組以「功能授權通過即可操作測資」為準，不驗生產級資料隔離。
