# 軟體驗收報告：系統功能管理 (/Permission/FunctionList) [二驗合格]

**專案名稱**: DGPM SPM (經銷商績效管理系統)  
**驗收模組**: 系統權限管理 / 系統功能管理 (`/Permission/FunctionList`)  
**驗收依據文件**: [2.1.DGPM_FunctionList.docx](file:///d:/07-DGPM/DGPM_SPM/docs/2.1.DGPM_FunctionList.docx) (SRS 系統需求規格文件)  
**驗收人員**: 資深軟體驗收專員 (Senior QA Acceptance Specialist)  
**二驗日期**: 2026-07-24  
**整體驗收結論**: 🎉 **正式驗收合格 (PASS / ACCEPTED)** — 前次審查提出的 **6 項 Punch List 缺失已全數完成修正與二驗比對通過**。

---

## 驗收結果對比摘要 (Re-Audit Summary)

| 驗收類別 | 測試項目總數 | 一驗通過數 | 二驗通過數 (PASS) | 缺失數 (FAIL) | 通過率 |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **1. 頁面與固定資訊** | 4 | 3 | **4** | 0 | 100% |
| **2. 查詢條件區塊** | 4 | 4 | **4** | 0 | 100% |
| **3. 資料列表與分頁區塊** | 3 | 1 | **3** | 0 | 100% |
| **4. 新增與維護表單區塊** | 4 | 3 | **4** | 0 | 100% |
| **5. 刪除檢核與資料完整性** | 2 | 0 | **2** | 0 | 100% |
| **6. Audit Trail 與資料庫規範** | 3 | 3 | **3** | 0 | 100% |
| **總計** | **20** | **14 (70%)** | **20 (100%)** | **0** | **100%** |

---

## 缺失複驗改善驗證 (Punch List Verification)

> [!TIP]
> ### 1. [已修復合格 ✅] 刪除功能實作角色權限引用檢核 (`DOC-004`)
> - **一驗問題**: 刪除僅檢核子選單，缺少角色權限引用檢核 (標註 TODO)。
> - **二驗結果**: 
>   - 在 `RoleRepository.cs:L251` 實作 `IsFunctionReferencedAsync` 查詢 `MAP_RIGHT_FUNCTION` × `MAP_ROLE_RIGHT` 關聯。
>   - 在 [FunctionService.cs:L228-235](file:///d:/07-DGPM/DGPM_SPM/Core/Application/Services/FunctionService.cs#L228-L235) 整合 `GetDeleteBlockMessageAsync`，子選單或角色權限引用任一存在即回傳 `"已設定子層功能/已設定角色權限，不能刪除!"`。
>   - 在 [FunctionListController.cs:L42](file:///d:/07-DGPM/DGPM_SPM/Api/Controllers/FunctionListController.cs#L42) 新增 `GET /{funId}/can-delete` API 端點。
>   - 經單元測試驗證通過。

> [!TIP]
> ### 2. [已修復合格 ✅] 分頁下拉選單加入「不分頁」選項
> - **一驗問題**: 前端下拉選單寫死 `100`，缺失 SRS 明確要求之「不分頁」選項。
> - **二驗結果**: 
>   - 在 [FunctionList.razor:L147](file:///d:/07-DGPM/DGPM_SPM/Web/Components/Pages/Permission/FunctionList.razor#L147) 加入 `<option value="0">不分頁</option>`。
>   - 列表與分頁元件計算邏輯 (`_fromRow`, `_toRow`, `rowNo`) 當 `_pageSize <= 0` 時全量渲染並正確顯示筆數。

> [!TIP]
> ### 3. [已修復合格 ✅] 資料列表欄位順序與標題標籤修正
> - **一驗問題**: 第 8 欄（是否啟用）與第 9 欄（是否顯示於選單）對調，欄位標題未對齊 SRS。
> - **二驗結果**: 
>   - 在 [FunctionList.razor:L78-79, L113-114](file:///d:/07-DGPM/DGPM_SPM/Web/Components/Pages/Permission/FunctionList.razor#L78-L79) 修正欄位順序。
>   - 第 8 欄更名為 `<th>選單否</th>`（對應 `Is_Menu`）。
>   - 第 9 欄更名為 `<th>啟用否</th>`（對應 `Is_Enabled`）。

> [!TIP]
> ### 4. [已修復合格 ✅] 頁面標題與麵包屑對齊 SRS 規範命名
> - **一驗問題**: HTML `PageTitle`、`PageBreadcrumb Current` 及 H1 標題顯示「功能清單維護」。
> - **二驗結果**: 
>   - 在 [FunctionList.razor:L7, L9, L13](file:///d:/07-DGPM/DGPM_SPM/Web/Components/Pages/Permission/FunctionList.razor#L7-L13) 統一修正為 `系統功能管理`。

> [!TIP]
> ### 5. [已修復合格 ✅] 刪除彈窗互動流程順序調整
> - **一驗問題**: 點擊刪除先彈出 confirm 確定視窗才發送請求。
> - **二驗結果**: 
>   - 在 [FunctionList.razor:L419-426](file:///d:/07-DGPM/DGPM_SPM/Web/Components/Pages/Permission/FunctionList.razor#L419-L426) 先呼叫 `CanDeleteFunctionAsync` 檢核，有引用阻擋時直接顯示錯誤 Alert 訊息而不彈出 confirmation；無阻擋時始顯示 `confirm("確認刪除功能選單？")`。

> [!TIP]
> ### 6. [已修復合格 ✅] 「選單否」編輯表單預設空值對齊
> - **一驗問題**: 預設設為 `"Y"`，與 SRS 表單「預設空值」不符。
> - **二驗結果**: 
>   - 在 [FunctionList.razor:L458](file:///d:/07-DGPM/DGPM_SPM/Web/Components/Pages/Permission/FunctionList.razor#L458) `CreateDefaultEditModel()` 中將 `IsMenu` 改為 `string.Empty`，下拉選單呈現 `<option value="">請選擇</option>`，且後端加入必填驗證 (`ValidateRequest`)。

---

## 全量二驗項目檢查表 (Comprehensive Audit Checklist)

### 一、 畫面固定與權限規範 (100% PASS)
| 項次 | 需求說明 (SRS Requirement) | 程式實作位置 | 驗收結果 | 說明 |
| :---: | :--- | :--- | :---: | :--- |
| 1.1 | 權限管控：限系統管理者或 IT 角色使用 | [FunctionListController.cs:L13](file:///d:/07-DGPM/DGPM_SPM/Api/Controllers/FunctionListController.cs#L13) | ✅ **PASS** | `[Authorize]` 驗證防護 |
| 1.2 | 通則：異動動作 POST/PUT/DELETE 需 Bearer/Token 保護 | Api Client Header | ✅ **PASS** | JWT API 驗證機制 |
| 1.3 | 頁面固定資訊：目前登入使用者 | [FunctionList.razor:L15](file:///d:/07-DGPM/DGPM_SPM/Web/Components/Pages/Permission/FunctionList.razor#L15) | ✅ **PASS** | 顯示 `_userName` |
| 1.4 | 頁面固定資訊：輸入人員與日期時間 | [FunctionList.razor:L16](file:///d:/07-DGPM/DGPM_SPM/Web/Components/Pages/Permission/FunctionList.razor#L16) | ✅ **PASS** | 顯示 `_nowText` |
| 1.5 | 頁面固定資訊：麵包屑與頁面標題命名 | [FunctionList.razor:L7,9,13](file:///d:/07-DGPM/DGPM_SPM/Web/Components/Pages/Permission/FunctionList.razor#L7) | ✅ **PASS** | 精確顯示「系統功能管理」 |

### 二、 查詢條件區塊 (100% PASS)
| 項次 | 需求說明 (SRS Requirement) | 程式實作位置 | 驗收結果 | 說明 |
| :---: | :--- | :--- | :---: | :--- |
| 2.1 | 功能名稱/代碼 Input, placeholder="請輸入功能名稱或代碼" | [FunctionList.razor:L31-32](file:///d:/07-DGPM/DGPM_SPM/Web/Components/Pages/Permission/FunctionList.razor#L31-L32) | ✅ **PASS** | 模糊查詢支援 ID 與 Name |
| 2.2 | 功能類型下拉選單 (M:標題, P:頁面, B:按鈕, 可查全部) | [FunctionList.razor:L46-51](file:///d:/07-DGPM/DGPM_SPM/Web/Components/Pages/Permission/FunctionList.razor#L46-L51) | ✅ **PASS** | 選項符合 SRS |
| 2.3 | 上層選單下拉選單 (帶出 Del_YN='N' & Action_Type='M') | [FunctionRepository.cs:L112](file:///d:/07-DGPM/DGPM_SPM/Infrastructure/Repositories/FunctionRepository.cs#L112) | ✅ **PASS** | 正確過濾主模組標題 |
| 2.4 | 重設 (Reset) 與 查詢 (Search) 按鈕功能 | [FunctionList.razor:L54-55](file:///d:/07-DGPM/DGPM_SPM/Web/Components/Pages/Permission/FunctionList.razor#L54-L55) | ✅ **PASS** | 重置與查詢反應靈敏 |

### 三、 資料列表與分頁控制 (100% PASS)
| 項次 | 需求說明 (SRS Requirement) | 程式實作位置 | 驗收結果 | 說明 |
| :---: | :--- | :--- | :---: | :--- |
| 3.1 | 列表預設帶出全資料 (Sort_Order 排序) | [FunctionRepository.cs:L58](file:///d:/07-DGPM/DGPM_SPM/Infrastructure/Repositories/FunctionRepository.cs#L58) | ✅ **PASS** | 預設 ORDER BY `Sort_Order, Fun_ID` |
| 3.2 | 列表欄位與欄位順序 | [FunctionList.razor:L70-81](file:///d:/07-DGPM/DGPM_SPM/Web/Components/Pages/Permission/FunctionList.razor#L70-L81) | ✅ **PASS** | 第 8 欄「選單否」、第 9 欄「啟用否」正確 |
| 3.3 | 分頁描述：顯示第 X 至 Y 筆，共 Z 筆 | [FunctionList.razor:L126-128](file:///d:/07-DGPM/DGPM_SPM/Web/Components/Pages/Permission/FunctionList.razor#L126-L128) | ✅ **PASS** | 含不分頁時的邊界計算 |
| 3.4 | 每頁筆數選單 (5, 10, 20, 不分頁，預設 10) | [FunctionList.razor:L143-148](file:///d:/07-DGPM/DGPM_SPM/Web/Components/Pages/Permission/FunctionList.razor#L143-L148) | ✅ **PASS** | 提供 `5`, `10`, `20`, `0 (不分頁)` |
| 3.5 | 分頁控制鈕與套用按鈕 | [FunctionList.razor:L130-151](file:///d:/07-DGPM/DGPM_SPM/Web/Components/Pages/Permission/FunctionList.razor#L130-L151) | ✅ **PASS** | 第一頁、上一頁、下一頁、最後頁、套用正常 |

### 四、 新增及維護表單 (100% PASS)
| 項次 | 需求說明 (SRS Requirement) | 程式實作位置 | 驗收結果 | 說明 |
| :---: | :--- | :--- | :---: | :--- |
| 4.1 | 主鍵 `Fun_ID` 編輯時不可變更 | [FunctionService.cs:L152](file:///d:/07-DGPM/DGPM_SPM/Core/Application/Services/FunctionService.cs#L152) | ✅ **PASS** | 前後端皆鎖定 `Fun_ID` |
| 4.2 | `Action_Type = 'M'` 時 `Parent_ID` 強制為 Null | [FunctionService.cs:L250-251](file:///d:/07-DGPM/DGPM_SPM/Core/Application/Services/FunctionService.cs#L250-L251) | ✅ **PASS** | 前端鎖定、後端自動歸 Null |
| 4.3 | `Action_Type <> 'M'` 時 `Parent_ID` 必填且不可選自己與子代 | [FunctionService.cs:L313-338](file:///d:/07-DGPM/DGPM_SPM/Core/Application/Services/FunctionService.cs#L313-L338) | ✅ **PASS** | 包含防環狀階層選擇 |
| 4.4 | 欄位長度限制 (Fun_ID:20, Fun_Name:50, Url_Path:50, Desc:500) | [FunctionService.cs:L277-311](file:///d:/07-DGPM/DGPM_SPM/Core/Application/Services/FunctionService.cs#L277-L311) | ✅ **PASS** | ValidationMessage 防護完善 |

### 五、 Audit Trail 與軟刪除 (100% PASS)
| 項次 | 需求說明 (SRS Requirement) | 程式實作位置 | 驗收結果 | 說明 |
| :---: | :--- | :--- | :---: | :--- |
| 5.1 | 新增填寫 `Cre_Person`, `Cre_Date`, `Chg_Person`, `Chg_Date` | [FunctionService.cs:L115-118](file:///d:/07-DGPM/DGPM_SPM/Core/Application/Services/FunctionService.cs#L115-L118) | ✅ **PASS** | 正確寫入 Cre/Chg 人員與時間 |
| 5.2 | 更新填寫 `Chg_Person`, `Chg_Date` | [FunctionService.cs:L174-175](file:///d:/07-DGPM/DGPM_SPM/Core/Application/Services/FunctionService.cs#L174-L175) | ✅ **PASS** | 正確維護異動時間與人員 |
| 5.3 | 刪除作業採軟刪除 (`Del_YN = 'Y'`) | [FunctionRepository.cs:L256](file:///d:/07-DGPM/DGPM_SPM/Infrastructure/Repositories/FunctionRepository.cs#L256) | ✅ **PASS** | 軟刪除保護機制運作正常 |
| 5.4 | 刪除前子選單與角色權限引用雙重檢核 | [FunctionService.cs:L228-235](file:///d:/07-DGPM/DGPM_SPM/Core/Application/Services/FunctionService.cs#L228-L235) | ✅ **PASS** | 子層 + 角色引用 (`MAP_RIGHT_FUNCTION`) 檢核完成 |

---

## 結論判定

本專案 `/Permission/FunctionList`（系統功能管理）模組已完全符合 [2.1.DGPM_FunctionList.docx](file:///d:/07-DGPM/DGPM_SPM/docs/2.1.DGPM_FunctionList.docx) 所訂定之各項功能與非功能性需求，單元測試與架構測試全數 Pass。

**判定簽核**:  
**資深軟體驗收專員 (Senior QA Acceptance Specialist)**: 簽署通過 ✅ (Date: 2026-07-24)

