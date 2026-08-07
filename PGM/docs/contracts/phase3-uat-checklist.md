# Phase 3 聯調驗收清單（開發期）

環境：PGM Api `http://localhost:9528`、PGM Web `8965`；DGPM 指向同一 JWT Secret；`Auth:AllowPGMLoginEntry=true`、`SystemCode=DGPM`。

前置：已執行 `SQL/10`、`20`、`90_dev_seed_admin.sql`；測資 `Admin`（系統管理員，預設 `DGPMAdmin`）或 `AshtonHsu`。

| # | 項目 | 結果 |
|---|---|---|
| 1 | DGPM 登入頁呼叫 PGM Login（body 含 `systemCode=DGPM`）成功 | ☐ |
| 2 | 登入失敗（錯密）→ `AUTH_INVALID`，不洩漏帳號是否存在 | ☐ |
| 3 | 無 DGPM 角色帳號 → `AUTH_NO_ROLE`（僅有 `PGMAdmin` 不算） | ☐ |
| 4 | 預設密碼強制改密（若測資為 0000） | ☐ |
| 5 | 已登入改密成功；歷程只在 PGM | ☐ |
| 6 | PGM 停用帳號後無法再登入；短效 Token 過期／me 失敗後清 session | ☐ |
| 7 | 角色變更（PGM RoleFunction）後，DGPM 重載選單可見變化（≤15 分或重登） | ☐ |
| 8 | JWT 過期後導回登入 | ☐ |
| 9 | 未授權功能不可進入 | ☐ |
| 10 | `DGPMAdmin` 側欄含業務模組＋`RoleKPIList`（KPI 下）；**無**系統權限管理／PgmAuthLink／帳號角色 CRUD | ☐ |
| 11 | DGPMAdmin／Uploader／Reviewer 選單差異正確（父層 M 自動顯示） | ☐ |
| 12 | KPI Excel 上傳／匯入預覽 | ☐ |
| 13 | KPI 匯入紀錄查詢 | ☐ |
| 14 | KPI 覆核／解鎖 | ☐ |
| 15 | PGM Web／API 可查登入紀錄 | ☐ |
| 16 | `AllowPGMLoginEntry=false` 時 DGPM 登入入口拒絕 | ☐ |
| 17 | PGM Api 停止時 DGPM 無法登入 | ☐ |

契約：[`auth-consumer-contract.md`](../contracts/auth-consumer-contract.md)  
資料範圍（未實作過濾）：[`data-scope-emp-org.md`](../contracts/data-scope-emp-org.md)
