# FunctionList（系統功能管理）規格比對與測試報告

| 項目 | 內容 |
| :--- | :--- |
| 專案 | PGM |
| 規格 | `docs/2.1.DGPM_FunctionList.docx`（V1.0 / 2026-07-22） |
| 比對日 | 2026-07-24 |
| 原型輔證 | `d:\07-DGPM\DGPM_HTML\Permission\FunctionList.html` |
| 既有驗收 | `tests/acceptance_report_functionlist.md`（二驗合格） |
| 結論摘要 | **核心 CRUD／查詢／分頁／刪除檢核／Audit Trail 已符合**；少數通則訊息字串、選單動態來源、Pager 全站導入屬部分符合或待釐清 |

---

## 1. 規格摘要（可驗收需求大項）

1. **定位與權限**：系統功能選單維護（`SysFun`）；屬系統權限管理；建議限管理者／IT；依角色決定可用功能。
2. **通則**：異動需防護（文件寫 Anti-Forgery）；連線確實關閉；新增寫入 Cre_*＋Chg_*；更新寫入 Chg_*；刪除前無子層、無角色權限引用。
3. **畫面共用**：左側選單共用元件、六大功能群組可展開／收合、目前項目高亮；分頁為共用元件（5／10／20／不分頁，預設 10，需按「套用」）；麵包屑、標題、登入者、輸入人員／日期。
4. **查詢**：關鍵字（名稱或代碼）、上層選單（`Action_Type='M'`）、功能類型（M／P／B／全部）、重設／查詢。
5. **列表**：欄位順序（操作、序、代碼、名稱、上層、類型、階層序號、選單否、啟用否、說明）；編輯帶入表單；刪除軟刪＋雙重檢核與指定訊息。
6. **表單**：Fun_ID 主鍵不可改；M→Parent 強制 Null；非 M→Parent 必填且不可選自己；必填／長度；選單否預設空、啟用否預設 N；Url／說明／Icon 可 NULL。
7. **錯誤處理**：讀取失敗／更新失敗／刪除失敗之指定訊息文案。

---

## 2. 比對結果表

狀態說明：**已符合**／**部分符合**／**不符合**／**文件未寫但已做**

| # | 需求 | 狀態 | 證據路徑 |
| :---: | :--- | :---: | :--- |
| R01 | 頁面標題／麵包屑為「系統功能管理」 | 已符合 | `Web/Components/Pages/Permission/FunctionList.razor`（PageTitle／H1／Breadcrumb）；對照 HTML 原型仍寫「功能清單維護」—**以 SRS 為準** |
| R02 | 目前登入使用者 | 已符合 | `Web/Components/Layout/MainLayout.razor`（top-row `user-name`） |
| R03 | 輸入人員／輸入日期 | 已符合 | `FunctionList.razor` L14–16 |
| R04 | 麵包屑：首頁 〉 上層模組 〉 本功能 | 已符合 | `Web/Components/PageBreadcrumb.razor` |
| R05 | 左側選單共用＋六大群組命名 | 已符合 | `Web/Navigation/NavMenuItems.cs`、`Web/Components/Layout/NavMenu.razor` |
| R06 | 功能群組展開／收合＋目前高亮 | 已符合 | `NavMenu.razor`（`ToggleGroup`／`isOpen`／`IsActiveGroup`／NavLink） |
| R07 | 選單資料來自 SysFun（IS_Menu／Is_Enabled／Del_YN） | 部分符合 | 結構硬編碼於 `NavMenuItems`；可見性依 `/api/auth/menus`（SysFun Fun_ID）過濾。非文件 SQL 動態組樹 |
| R08 | 分頁共用元件（筆數範圍、前後頁、5／10／20／不分頁、套用） | 已符合（本頁） | `Web/Components/Shared/Pager.razor`；`FunctionList.razor` L124–133。**全站其他頁尚未改用 Pager** → 若以「所有頁面皆用」驗收則為部分符合 |
| R09 | 查詢：關鍵字／上層 M／類型／重設／查詢 | 已符合 | `FunctionList.razor`；`FunctionRepository.GetPagedAsync`；`GetModuleOptionsAsync`（`Action_Type='M'`） |
| R10 | 列表欄位順序：選單否 → 啟用否 | 已符合 | `FunctionList.razor` L67–77、L113–114（優於 HTML 原型欄位對調） |
| R11 | 新增 Reset 表單；編輯帶入；Fun_ID 鎖定 | 已符合 | `StartCreateAsync`／`StartEditAsync`；後端 `UpdateAsync` 拒絕改碼 |
| R12 | 刪除：先檢核子層＋角色引用，再 confirm；軟刪 | 已符合 | `DeleteAsync`；`FunctionService.GetDeleteBlockMessageAsync`；`RoleRepository.IsFunctionReferencedAsync`；`can-delete` API |
| R13 | 刪除阻擋訊息「已設定子層功能/已設定角色權限，不能刪除!」 | 已符合 | `FunctionService.cs` L232；前端 fallback 同文案 |
| R14 | Action_Type=M → Parent Null；非 M 必填；不可選自己／子孫 | 已符合 | `FunctionService.NormalizeRequest`／`ValidateParentAsync` |
| R15 | 欄位長度 Fun_ID20／Name50／Url50／Desc500；選單否空預設；啟用否預設 N | 已符合 | `ValidateRequest`；`CreateDefaultEditModel` |
| R16 | Audit Trail Cre_*／Chg_*；Del_YN 軟刪 | 已符合 | `CreateAsync`／`UpdateAsync`／`SoftDeleteAsync`；`FunctionRepository` |
| R17 | 表單不強制維護 Icon（可 NULL） | 已符合 | 畫面無 Icon；Insert 寫 `Icon=null`；對齊 TableList「暫不設定」（DOC-008） |
| R18 | 權限：僅管理者／IT | 部分符合 | API `[Authorize]` JWT；選單／功能權限靠角色 menus。無專屬「IT 角色」硬碼檢核 |
| R19 | 異動需 Anti-Forgery Token | 部分符合 | Blazor `UseAntiforgery`；API 寫入採 JWT Bearer（架構替代，非文件字面 CSRF Token） |
| R20 | 錯誤訊息固定文案（無法讀取／資料庫更新失敗…） | 不符合 | 前端多用 API／通用訊息（如「操作失敗，請稍後再試。」）；未對應 SRS 2.2.6 三句原文 |
| R21 | 上層選單「可輸入文字的下拉」 | 部分符合 | 一般 `<select>`／`InputSelect`，無 typeahead 輸入 |
| R22 | 配色對齊 HTML 原型 | 已符合（輔證） | `--color-primary: #0b63d1` 等見 `Web/wwwroot/app.css` 與 `DGPM_HTML/Assets/Css/Common.css`；hover `#f4f9ff`、編輯／刪除 icon 色一致 |
| R23 | 種子／模組 Fun_ID 命名 | 已符合 | `SQL/15_dbo_sysfun.sql`（Permission／FunctionList 等） |
| R24 | 防環狀階層、Parent='0'→null | 文件未寫但已做 | `IsDescendantAsync`；`NormalizeParentId`（定案頂層僅 NULL） |
| R25 | `can-delete` 預檢 API | 文件未寫但已做 | `FunctionListController` GET `{funId}/can-delete` |

---

## 3. 測試執行結果

指令：

```text
dotnet test tests\Core.Tests\PGM.Core.Tests.csproj
  --filter "FullyQualifiedName~Function"
  -o tests\_test_out_functionlist
```

| 項目 | 結果 |
| :--- | :--- |
| 執行狀態 | **通過**（exit code 0） |
| 通過數 | **18 / 18** |
| 失敗數 | 0 |
| 說明 | filter 含 `FunctionServiceTests`（12）及名稱含 Function 的 Role／Permission 相關案例（6） |
| 輸出目錄 | `tests/_test_out_functionlist`（避開 Web bin 鎖定） |
| Architecture.Tests 同 filter | 無相符案例（exit 0） |

`FunctionServiceTests` 覆蓋重點：分頁映射、類型檢核、新增／M 清 Parent、'0' 正規化、重複碼、Fun_ID 不可改、子孫不可當上層、子層／角色引用刪除阻擋、`CanDelete`、軟刪 Commit／Rollback。

---

## 4. 缺口與建議優先順序

| 優先 | 項目 | 建議 |
| :---: | :--- | :--- |
| P1 | R20 錯誤訊息文案 | 將讀取／寫入失敗對應 SRS「無法讀取資料，請再確認。」「資料庫更新失敗，其他功能不受影響。」（或請 SA 同意沿用現行通用文案並改文件） |
| P2 | R08 Pager 全站導入 | 其他列表頁改用 `Shared/Pager`，滿足「供所有 DGPM 頁面呼叫」 |
| P3 | R07 選單動態化 | 評估改由 SysFun（IS_Menu／Is_Enabled）組樹，減少硬編碼與種子雙軌 |
| P4 | R18／R19 | 與 SA 確認：JWT＋功能權限是否視為 Anti-Forgery／管理者管控之等效實作；必要時補頁級權限（Fun_ID=FunctionList） |
| P5 | R21 可輸入下拉 | 若需嚴格符合，再做 combobox；否則請文件改為「下拉選單」 |

**非缺口說明（刻意對齊 SRS 而非原型）**

- 頁面標題用「系統功能管理」，不用 HTML「功能清單維護」。
- 列表欄位「選單否／啟用否」順序依 SRS，不依 HTML「是否啟用／是否顯示於選單」。

---

## 5. 與既有驗收報告關係

`tests/acceptance_report_functionlist.md`（2026-07-24 二驗）所列 6 項 Punch List（刪除角色引用、不分頁、欄位順序、標題、刪除流程、選單否預設空）經本次靜態比對與單元測試，**仍維持通過**。本報告補充：選單資料來源、錯誤文案、Pager 全站採用、JWT／Anti-Forgery 等效性等**文件通則／跨頁**項目之部分符合狀態。

---

## 6. 主要實作索引

| 層 | 路徑 |
| :--- | :--- |
| UI | `Web/Components/Pages/Permission/FunctionList.razor` |
| Pager | `Web/Components/Shared/Pager.razor` |
| Nav | `Web/Components/Layout/NavMenu.razor`、`Web/Navigation/NavMenuItems.cs` |
| API | `Api/Controllers/FunctionListController.cs` |
| Service | `Core/Application/Services/FunctionService.cs` |
| Repo | `Infrastructure/Repositories/FunctionRepository.cs` |
| 角色引用 | `Infrastructure/Repositories/RoleRepository.cs`（`IsFunctionReferencedAsync`） |
| 種子 | `SQL/15_dbo_sysfun.sql` |
| 單元測試 | `tests/Core.Tests/Services/FunctionServiceTests.cs` |
