# DGPM_SPM｜SA 文件問題與缺件清單

| 項目 | 內容 |
| --- | --- |
| **專案** | DGPM_SPM（經銷商績效管理） |
| **收件對象** | SA（Solution Architect） |
| **文件日期** | 2026-07-23 |
| **目的** | 彙整目前 SA 交付文件之現況與缺口，懇請協助確認、定案與補件，俾開發得以依正式規格推進，避免再依假設擴充資料表或畫面。 |
| **參考文件** | `2.1.DGPM_FunctionList.docx`、`DGPM_TableList.xlsx`（及轉換檔 `DGPM_TableList.md` / `DGPM_TableList.raw.md` / `DGPM_TableList_quality.json`）、`DGPM_HTML`（靜態切版） |

---

## 1. 核心結論

經對照 FunctionList、TableList 與 HTML 切版，目前文件現況可歸納如下：

1. **`DGPM_TableList` 目前僅定義 `SysFun`（系統功能設定檔）一張表**，不足以支撐 FunctionList 中列出的完整 14 個葉功能。
2. **14 個葉功能中，目前僅「系統功能管理（FunctionList）」可與既有文件較完整對應**（SRS：`2.1.DGPM_FunctionList.docx`；資料表：`SysFun`；畫面範例：`DGPM_HTML/Permission/FunctionList.html`）。
3. 其餘 13 個葉功能**尚缺對應的 TableList／欄位定義／關聯與檢核規格**；在補齊前，資料面無法依 SA 正式規格建表與驗收。
4. **HTML README 對 DB 命名採「建議」用語**（`SysMenu`／`title|menu|button`），與 TableList／FunctionList 正式規格（`SysFun`／`M|P|B`）不一致（詳見 DOC-006）；建議以 **TableList／FunctionList 為資料與規則真相來源**，HTML 作為**畫面範例**（非資料規格）。

> 說明：本清單僅描述「文件現況／需補齊事項」，不涉及實作路線或程式現況指責；示意表名凡標「待 SA 定名」者，均請 SA 最終定名後回覆。

---

## 2. 葉功能 × SysFun 支撐能力對照表

模組／功能命名依 FunctionList.docx 與業務確認之命名方式。  
「SysFun 能否支撐」僅評估**現有 TableList 已交付之 `SysFun`** 是否足以作為該功能的主要資料來源。

| # | 模組代碼 | 葉功能 | 功能代碼 | SysFun 能否支撐 | 缺哪些表（示意，待 SA 定名） | 文件現況 |
| --- | --- | --- | --- | --- | --- | --- |
| 1.1 | Masterdata | 經銷商設定管理 | DealerList | 否 | 經銷商主檔等（待 SA 定名） | 缺 TableList／專屬 SRS |
| 1.2 | Masterdata | 區域組織管理 | OrgList | 否 | 區域／組織主檔等（待 SA 定名） | 缺 TableList／專屬 SRS |
| 2.1 | Permission | 系統功能管理 | FunctionList | **是（主要）** | 刪除前「角色引用」檢核用表（待 SA 定名；見 DOC-004） | SRS＋`SysFun`＋HTML 範例已有 |
| 2.2 | Permission | 角色與權限管理 | RoleFunList | 否（僅可被選單引用） | 角色主檔、角色×功能對照等（待 SA 定名） | 缺 TableList／專屬 SRS |
| 2.3 | Permission | KPI 資料權限設定 | RoleKPIList | 否 | 角色×KPI／資料範圍對照等（待 SA 定名） | 缺 TableList／專屬 SRS |
| 2.4 | Permission | 使用者帳號管理 | Accounts | 否 | 使用者主檔、帳號×角色等（待 SA 定名） | 缺 TableList／專屬 SRS |
| 3.1 | SysConfig | 匯率參數設定 | ExchangeRates | 否 | 匯率／參數設定檔等（待 SA 定名） | 缺 TableList／專屬 SRS |
| 4.1 | KPIIndicator | KPI 指標設定 | KPIManage | 否 | KPI 指標主檔等（待 SA 定名） | 缺 TableList／專屬 SRS |
| 4.2 | KPIIndicator | KPI 數據匯入 | KPIImport | 否 | 匯入主檔／明細／暫存等（待 SA 定名） | 缺 TableList／專屬 SRS；匯入格式待補 |
| 4.3 | KPIIndicator | KPI 數據覆核與解鎖 | KPIImpReview | 否 | 覆核狀態／鎖定紀錄等（待 SA 定名） | 缺 TableList／專屬 SRS |
| 5.1 | Syslog | KPI 異動紀錄查詢 | KPIChgLog | 否 | KPI 異動紀錄表等（待 SA 定名） | 缺 TableList／專屬 SRS |
| 5.2 | Syslog | KPI 匯入日誌查詢 | KPIImpLog | 否 | KPI 匯入日誌表等（待 SA 定名） | 缺 TableList／專屬 SRS |
| 5.3 | Syslog | 使用者登入軌跡查詢 | KPIAccLog | 否 | 登入軌跡／認證日誌表等（待 SA 定名） | 缺 TableList／專屬 SRS |
| 6.1 | Dashboard | Qlik Cloud | RdtQlik | 否（通常非核心業務表） | 整合設定／權限／嵌入參數等（待 SA 定名，或確認無表） | 缺整合規格／設定定義 |

**統計摘要**

| 項目 | 數量 |
| --- | --- |
| 葉功能總數 | 14 |
| 現有 TableList 可主要支撐 | 1（FunctionList／`SysFun`） |
| 尚缺正式表結構／規格 | 13（含 Dashboard 是否需表待確認） |

---

## 3. 問題清單（DOC-xxx）

嚴重度定義：

| 等級 | 意義 |
| --- | --- |
| **Blocker** | 未回覆／未補件前，無法依 SA 規格推進對應範圍之建表或驗收 |
| **High** | 嚴重影響正確性或工期，宜優先回覆 |
| **Medium** | 影響實作細節或跨文件一致性，需定案 |
| **Low** | 可後補，不阻塞目前主路徑 |

---

### DOC-001｜TableList 僅含 SysFun，無法覆蓋 14 葉功能

| 欄位 | 內容 |
| --- | --- |
| **嚴重度** | Blocker |
| **核對來源** | 已核對 `docs/DGPM_TableList.xlsx`（openpyxl）：**2 個 sheet**＝`TableList`、`SysFun`。`TableList` sheet 清單列僅 1 筆資料表；`SysFun` sheet 為該表欄位定義。轉換檔 `DGPM_TableList.md`／`DGPM_TableList_quality.json`（`openxml_sheets.count: 2`）與 xlsx 一致。佐證：`2.1.DGPM_FunctionList.docx`「相關Table」亦僅列 `SysFun`。 |
| **原文引用** | |
| | **TableList（xlsx／md）表清單** |
| | > \| 1 \| SysFun \| 系統功能設定檔 \| |
| | **FunctionList.docx「相關Table」** |
| | > \| 1 \| 系統功能設定檔 \| SysFun \| CRUD \| 限系統管理者/IT 角色維護 \| |
| **描述** | 已交付之 TableList 僅定義 `SysFun` 一張表。除系統功能管理外，其餘葉功能尚無正式資料表與欄位規格。 |
| **影響** | 無法依 SA「有表才建表」原則完成 Masterdata／其餘 Permission／Config／KPI／Log／Dashboard 之資料面設計與驗收。 |
| **請 SA 回覆** | 其餘葉功能之 TableList（表清單＋欄位定義）預估交付時程？是否分批補件？批次順序是否同意本文件第 4 節建議？ |

---

### DOC-002｜除 FunctionList 外，其餘葉功能缺專屬 SRS／操作規格

| 欄位 | 內容 |
| --- | --- |
| **嚴重度** | Blocker |
| **來源** | 目前可見專屬 SRS：`2.1.DGPM_FunctionList.docx`；其餘功能代碼見該文件模組命名表，但無對應完整章節／獨立 SRS |
| **描述** | 14 葉功能中，僅 FunctionList 具備較完整之畫面區段、查詢／列表／新增修改、檢核與錯誤訊息描述。 |
| **影響** | 其餘功能無法定義欄位檢核、CRUD 規則、分頁與例外處理之驗收標準。 |
| **請 SA 回覆** | 其餘 13 功能是否會陸續提供與 `2.1` 同格式之 SRS？優先順序？ |

---

### DOC-003｜登入／使用者／角色相關表未出現於 TableList

| 欄位 | 內容 |
| --- | --- |
| **嚴重度** | Blocker |
| **來源** | TableList 無使用者／角色／對照表；FunctionList 敘述權限依登入帳號所屬角色決定可用功能 |
| **描述** | 系統需登入與角色才能運作選單與權限，但 TableList 現況未定義對應表。 |
| **影響** | 無法正式定案認證／帳號／角色資料模型；影響 Accounts、RoleFunList 與選單權限聯動之規格封閉。 |
| **請 SA 回覆** | 使用者、角色、角色×功能等表是否另行補件？表名與關鍵欄位？在補件前是否允許「暫用最小登入骨架（需標註非 SA 正式表）」僅供開發聯調？ |

---

### DOC-004｜FunctionList 刪除檢核「角色引用」對應表未定義

| 欄位 | 內容 |
| --- | --- |
| **嚴重度** | High |
| **來源** | `2.1.DGPM_FunctionList.docx`（刪除前需確認無子層功能、無角色權限設定；角色檢核「待規格補」語意） |
| **描述** | 軟刪（`Del_YN='Y'`）前需檢核角色是否已引用該功能，但 TableList 尚無角色／權限對照表。 |
| **影響** | FunctionList 刪除規則無法完整實作與驗收；僅能先做「無子層」檢核，角色引用部分需等表。 |
| **請 SA 回覆** | 角色引用檢核對應哪一（些）張表？關鍵欄位與判斷條件？本階段可否先僅檢核子層？ |

---

### DOC-005｜頂層 `Parent_ID`：TableList 與 FunctionList 填值差異需定案

| 欄位 | 內容 |
| --- | --- |
| **嚴重度** | High（已定案） |
| **狀態** | **已定案：本專案頂層固定 NULL**（2026-07-24） |
| **核對來源** | 已核對 `DGPM_TableList.xlsx`／`DGPM_TableList.md` 之 `Parent_ID` 資料描述；以及 `2.1.DGPM_FunctionList.docx` 新增／維護說明與欄位表。 |
| **原文引用** | |
| | **TableList（xlsx SysFun sheet／md）— 有寫「0 or NULL」** |
| | > 上層選單 ID（頂層選單填 0 or NULL，如 PERMISSION） |
| | （欄位：`Parent_ID`，允許 NULL＝Y） |
| | **FunctionList.docx — 僅寫 Null，沒有「0」** |
| | > 若為功能類型='M'，將此欄位設為Null。 |
| | > 帶出功能名稱，若為功能類型=M，將此欄位設為Null，預設Null |
| | （全文未出現頂層填 `0` 之規則。） |
| **描述** | 此為 **TableList 與 FunctionList 兩份文件之間的差異**，**不是**單一文件內部自相矛盾。TableList 允許頂層為 `0` 或 `NULL`；FunctionList 僅規範 `Action_Type='M'` 時 `Parent_ID` 設為 Null。 |
| **定案** | **已定案：本專案頂層固定 NULL**（2026-07-24）。不用 `0`；`Action_Type='M'` 時 `Parent_ID` 設 NULL。實作／種子／API 正規化皆以 NULL 為準（誤傳 `'0'` 正規化為 NULL）。建議後續請 SA 修訂 TableList 說明刪除「0」。 |
| **影響** | 選單樹組裝、查詢條件統一為 `Parent_ID IS NULL`；勿再使用 `= '0'`。 |
| **請 SA 回覆** | （已定案）TableList 說明是否一併刪除「0」用語？（文件修訂建議，不阻塞實作） |

---

### DOC-006｜HTML README 之 DB 命名「建議」與正式規格不一致

| 欄位 | 內容 |
| --- | --- |
| **嚴重度** | Medium（調降：原文為「建議」，非強制規格衝突） |
| **核對來源** | 已核對 `d:\07-DGPM\DGPM_HTML\README.md`「後端轉換注意事項」；對照 TableList／FunctionList 之 `SysFun`、`Action_Type`。 |
| **原文引用** | |
| | **HTML README（建議用語，非強制規格）** |
| | > 實際 DB 欄位建議以 `SysMenu` 為主，功能類型對應 `Menu_Type`：`title`、`menu`、`button`。 |
| | **TableList（正式欄位）** |
| | > 功能類型 M (標題)、P (頁面)、B (按鈕) |
| | （表名：`SysFun`；欄位：`Action_Type`） |
| | **FunctionList.docx（同碼域）** |
| | > M (標題)、P (頁面)、B (按鈕) |
| **描述** | HTML README 以「建議」提出另一套命名（`SysMenu`／`Menu_Type`＝title｜menu｜button），與 TableList／FunctionList 已定之 `SysFun`／`Action_Type`＝M｜P｜B **不一致**。不宜過度解讀為「規格衝突」；宜定案哪一份為資料真相來源。 |
| **影響** | 若未定案，開發與驗收可能誤引 README 建議當作正式表／碼域。 |
| **請 SA 回覆** | 是否確認：**資料／規則以 TableList＋FunctionList 為準（`SysFun`、M／P／B）**；**HTML 僅作版面範例**？若需同步修正 README 建議句，是否由 SA／前端文件維護者更新？ |

---

### DOC-007｜`Url_Path` 長度 nvarchar(50) 與路由命名是否足夠

| 欄位 | 內容 |
| --- | --- |
| **嚴重度** | Medium |
| **核對來源** | 路由命名例 **僅見於 TableList**；FunctionList 有 `Url_Path` nvarchar(50) 欄位定義，但**無** `PERMISSION/DealerList` 此例。 |
| **原文引用** | |
| | **TableList（xlsx／md）— 僅此處有路由例** |
| | > 前端路由或 URL（如 PERMISSION 或 PERMISSION/DealerList） |
| | （欄位：`Url_Path`，nvarchar，長度 50，允許 NULL＝Y） |
| | **FunctionList.docx（欄位表）** |
| | > 前端路由或 URL \| SysFun \| Url_Path \| nvarchar \| 50 |
| | （無模組／功能路徑範例文字。） |
| **描述** | 長度與命名例目前以 TableList 為準；若未來路徑含較長模組前綴或 query，50 字元可能不足。 |
| **影響** | 路由無法入庫或需縮寫，造成前後端不一致。 |
| **請 SA 回覆** | 50 是否為最終長度？建議最大路徑範例為何？大小寫是否以 TableList 例（`PERMISSION/...`）為準？是否需調整長度？ |

---

### DOC-008｜`Icon` 欄位「暫不設定及使用」與 FunctionList 敘述需對齊

| 欄位 | 內容 |
| --- | --- |
| **嚴重度** | Low |
| **核對來源** | 已核對 TableList `SysFun.Icon`；FunctionList 維護說明、查詢 SELECT、列表欄位表、新增／維護欄位表。 |
| **原文引用** | |
| | **TableList（xlsx／md）— 同一欄 `SysFun.Icon`** |
| | > 選單圖示代碼（如 MasIcon, SecIcon），暫不設定及使用 |
| | （`Icon`，nvarchar(50)，允許 NULL＝Y） |
| | **FunctionList.docx — 文字允許 Icon 為 NULL（指 DB 欄）** |
| | > 欄位：上層選單、功能說明、前端路由或 URL、Icon可以為NULL，其他欄位均為必填。 |
| | **FunctionList.docx — 查詢／列表 SELECT 含 Icon 欄** |
| | > select Fun_ID,Fun_Name,Parent_ID,Action_Type,Url_path,Icon,Sort_Order,... |
| | **FunctionList.docx — 新增／維護「畫面欄位表」未列 Icon**（小缺口） |
| | 欄位表列有：功能代碼、功能名稱、上層選單、功能類型、Url_Path、階層序號、選單否、啟用否、說明及稽核欄；**無 `Icon` 列**。 |
| | **勿與列表「編輯Icon／刪除Icon」混淆**（此為操作圖示 UI，非 `SysFun.Icon` 欄） |
| | > 編輯Icon、刪除Icon |
| **描述** | TableList 與 FunctionList 所指皆為同一 DB 欄 `SysFun.Icon`：TableList 寫「暫不設定及使用」；FunctionList 文字允許 NULL，且 SELECT 帶出 Icon，但**新增／維護欄位表未列出 Icon**。需釐清本階段是否維護該欄。 |
| **影響** | 畫面範圍與驗收項目可能歧義（尤其「暫不使用」vs 文字仍提及可 NULL）。 |
| **請 SA 回覆** | 本階段 UI／API 是否完全不做選單圖示 `Icon`？欄位是否仍保留於 `SysFun`？FunctionList 欄位表是否應補列或自 SELECT／敘述中移除？ |

---

### DOC-009｜共用稽核欄位與安全規則之全系統適用性

| 欄位 | 內容 |
| --- | --- |
| **嚴重度** | Medium |
| **來源** | FunctionList：新增須寫入 Cre_*／Chg_*；更新須寫入 Chg_*；POST 需 Anti-Forgery；連線與 Audit Trail 原則 |
| **描述** | 上述規則目前寫在 FunctionList SRS；其他功能表尚未交付，是否一律沿用需確認。 |
| **影響** | 後續各表欄位命名（Cre_Person／Chg_Date 等）與安全要求若不一致，將增加重工。 |
| **請 SA 回覆** | Cre_*／Chg_*／Del_YN 等是否為全系統共用慣例？Anti-Forgery／權限檢核是否適用所有維護畫面？ |

---

### DOC-010｜Dashboard／Qlik Cloud 是否需要資料表或僅整合設定

| 欄位 | 內容 |
| --- | --- |
| **嚴重度** | Medium |
| **來源** | 功能清單含 `Dashboard`／`RdtQlik`；TableList 無對應表；HTML 亦無此頁切版 |
| **描述** | 儀表板可能為外部嵌入，不一定需要核心業務表，但仍需整合與權限規格。 |
| **影響** | 無法判斷交付物為「設定表＋嵌入」或「純外部連結／無表」。 |
| **請 SA 回覆** | RdtQlik 是否需要 TableList？若否，請提供整合方式、URL／權杖、權限與驗收標準。 |

---

### DOC-011｜KPI 匯入檔格式與覆核狀態碼域尚未定義

| 欄位 | 內容 |
| --- | --- |
| **嚴重度** | High（對 KPI 範圍為 Blocker） |
| **來源** | 功能清單含 KPIImport／KPIImpReview；尚無對應 TableList／檔案規格 |
| **描述** | 匯入欄位、檔案類型（Excel／CSV）、錯誤列處理、覆核／鎖定狀態碼均未見於現有 SA 文件。 |
| **影響** | KPI 匯入與覆核無法設計資料模型與驗收案例。 |
| **請 SA 回覆** | 匯入範本、必填欄位、狀態碼（草稿／已匯入／已覆核／鎖定等）與解鎖條件？ |

---

## 4. 建議補件順序

懇請 SA 依下列順序補齊 **TableList（表清單＋欄位）** 與（如適用）**對應 SRS／畫面說明**，以降低開發等待與重工：

| 順位 | 範圍 | 涵蓋葉功能（代碼） | 建議理由 |
| --- | --- | --- | --- |
| **1** | **Permission** | FunctionList（已有）、RoleFunList、RoleKPIList、Accounts | 選單、角色、帳號為全系統前置依賴；FunctionList 刪除檢核亦依賴角色引用表 |
| **2** | **Masterdata** | DealerList、OrgList | KPI 與多數報表／權限範圍通常依賴經銷商與組織主檔 |
| **3** | **KPI** | KPIManage、KPIImport、KPIImpReview | 核心業務作業；需指標定義＋匯入＋覆核狀態模型 |
| **4** | **Log** | KPIChgLog、KPIImpLog、KPIAccLog | 多由作業寫入；宜在 KPI／登入模型定案後補齊查詢欄位 |
| **5** | **Config** | ExchangeRates | 相對獨立，可於主資料與權限之後補件 |
| **6** | **Dashboard** | RdtQlik | 整合規格可後置；若無需業務表請明文確認 |

> 若貴單位內部排程不同，亦請回覆實際可交付順序，開發端將據以調整里程碑。

---

## 5. 第 0 步需先定案事項（命名／碼域／安全／欄位長度）

下列事項建議於大量補表與開發展開前先定案，作為後續所有 TableList／SRS 的共同約束：

### 5.1 命名

| 項目 | 建議確認點 |
| --- | --- |
| 模組／功能代碼 | 是否以 FunctionList 命名表為準（如 `Permission`／`FunctionList`、`Masterdata`／`DealerList`）？ |
| 資料表英文名 | 是否統一 PascalCase（如 `SysFun`）？Schema 是否使用 `dbo` 或其他？ |
| 路由／`Url_Path` | 是否採 `模組/功能`（例：`Permission/FunctionList`）？大小寫規則？ |
| 欄位命名風格 | 是否統一 `Fun_ID`、`Cre_Person`、`Del_YN` 這類風格於後續所有表？ |

### 5.2 碼域

| 項目 | 建議確認點 |
| --- | --- |
| `Action_Type` | 確認以 **M／P／B** 為準（HTML README 之 title／menu／button 僅為「建議」，見 DOC-006） |
| Y／N 欄位 | `Is_Menu`、`Is_Enabled`、`Del_YN` 等是否一律 `char(1)`＋`Y`／`N`？預設值是否全域一致（如 `Is_Enabled` 預設 N、`Del_YN` 預設 N）？ |
| 軟刪語意 | 刪除＝`Del_YN='Y'`，與「停用」`Is_Enabled='N'` 嚴格分開？ |
| 後續狀態碼 | KPI 覆核／匯入狀態等請於補件時一併定義碼表 |

### 5.3 安全

| 項目 | 建議確認點 |
| --- | --- |
| 認證與授權 | 登入帳號、角色、功能權限之資料來源表 |
| 寫入防護 | 維護類 POST 是否一律 Anti-Forgery（FunctionList 已要求） |
| Audit Trail | 所有表之 Insert／Update 是否強制 Cre_*／Chg_* |
| 敏感欄位 | 密碼雜湊、個資欄位長度與遮罩規則（Accounts 補件時尤需） |

### 5.4 欄位長度（以現有 SysFun 為例，請確認是否為全系統基準）

| 欄位（SysFun） | 型別／長度 | 請確認 |
| --- | --- | --- |
| `Fun_ID` | varchar(20) | 功能代碼長度是否足夠、是否允許修改（文件：新增後不可改） |
| `Fun_Name` | nvarchar(50) | 中文名稱長度 |
| `Parent_ID` | varchar(20) | 與 Fun_ID 同長；**已定案：本專案頂層固定 NULL**（2026-07-24；見 DOC-005；不用 0） |
| `Url_Path` | nvarchar(50) | 見 DOC-007 |
| `Fun_Desc` | nvarchar(500) | 說明長度 |
| `Cre_Person`／`Chg_Person` | nvarchar(50) | 人員 ID 或程式名稱長度 |
| `Sort_Order` | decimal(6,2) | 階層序號精度是否全系統沿用 |

---

## 6. 懇請 SA 回覆／補件之明確請求

為利專案依 SA 規格推進，懇請協助於方便時回覆下列事項（可直接於本文件批註或另附修訂檔）：

1. **確認核心結論**：同意「目前僅 FunctionList／`SysFun` 可完整對應；其餘 13 葉功能需補 TableList（及 SRS）」之文件現況判斷。  
2. **回覆 DOC-001～DOC-011**：至少優先處理 **Blocker／High** 項目。  
3. **提供補件時程**：依第 4 節順序（或貴單位調整後順序）回覆各批 TableList／SRS 預計日期。  
4. **定案第 0 步**：命名、碼域（含 M／P／B）、安全與共用欄位長度慣例。  
5. **同步文件（如需）**：若確認 HTML 僅為畫面範例，建議更新 `DGPM_HTML/README.md` 中「實際 DB 欄位建議以 SysMenu…」一句，避免後續誤引建議為正式規格。  
6. **Dashboard**：確認 `RdtQlik` 是否需要資料表；若否，請提供整合與驗收規格。

如有任何文件已在其他路徑交付、或本清單誤判現況之處，亦請指正，我們將立即更新對照基準。

---

**文件狀態**：草稿供轉寄 SA 確認／補件  
**編寫日期**：2026-07-23  
**專案**：DGPM_SPM
