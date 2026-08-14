# DGPM Auth 設定（一律外連 PGM）

契約：姊妹專案 [`PGM/docs/contracts/auth-consumer-contract.md`](../../PGM/docs/contracts/auth-consumer-contract.md)

> **Local Auth 已退役**：登入／帳號／角色／選單／登入歷程真相＝**PGM**。DGPM 經 `IPgmAuthClient` 轉發，不寫本地 Auth DB。

## 部署參數（Api `appsettings`／環境變數）

| Key | 說明 |
|---|---|
| `Auth__AllowPGMLoginEntry` | `true`／`false`（是否允許由 DGPM 登入頁進入；與 PgmUiMode **獨立**） |
| `Auth__PgmBaseUrl` | PGM **Api** |
| `Auth__PgmWebBaseUrl` | PGM **Web**（側欄外連／對稱設定） |
| `Auth__SystemCode` | 固定 `DGPM` |

## PgmUiMode（系統權限 UI 所在端）

| 項目 | 說明 |
|---|---|
| 單一真相 | PGM `SET_PARAM`：`SET_ITEM=Auth`，`SET_ID=PgmUiMode`，值 `On`｜`Off` |
| API | `GET/PUT /api/system/ui-mode`（DGPM 轉發 PGM；僅帳號掛 **PGMAdmin** 可寫） |
| Mode=`On` | 系統權限在 **PGM Web**；DGPM 選單不顯示【系統管理權限】 |
| Mode=`Off` | 系統權限在 **DGPM**（轉發 PGM AUTH API）；PGM Web AUTH 選單隱藏／寫入拒絕 |
| Fun | 沿用 `AUTH01`～`AUTH04`、`AUTH06`～`AUTH09`（`FUNCTION_ID` 全域唯一，MAP 給 `DGPMAdmin`；Mode=Off 時選單掛父層 `Permission`） |
| AUTH09 | `PUT /api/system/users/{userId}/reset-password` 代重設密碼 |

## 必對齊 JWT（與 PGM 相同）

- `JwtSettings__Issuer`／`Audience`／`SecretKey`（≥32；僅 User Secrets／環境變數）

## 側欄

選單真相＝PGM `GET /api/auth/menus`（依 JWT `rid`＋`sys`＋**PgmUiMode**）。Mode=Off 且角色 MAP 含 AUTH* 時，DGPM 會出現【系統管理權限】子頁。
