# Auth Consumer Contract（PGM → DGPM）

> 帳號／登入／JWT／選單真相來源＝**PGM**。DGPM（`AuthMode=PGM`）必須依本文件實作，不得自創 Claim 名稱或錯誤碼語意。  
> 測試機 Api：`http://localhost:9528`（健康檢查：`GET /api/health`）。

## 1. JWT（共用簽章，以 PGM 為主）

| 項目 | 值 |
|---|---|
| 演算法 | HS256 |
| Issuer／Audience／SecretKey | 與 PGM `JwtSettings` **完全相同**（Secret 僅環境變數／User Secrets） |
| AccessToken 有效期 | **10 分鐘**（`JwtSettings:AccessTokenMinutes`，開發期對齊 5～15 分失效期望） |
| RefreshToken | 回傳字串但**不落地**；`POST /api/auth/refresh` 回未授權。過期請重新登入。 |

### Claims

| Claim | 常數名 | 說明 |
|---|---|---|
| `uid` | `JwtClaimNames.UserId` | 使用者 ID |
| `rid` | `JwtClaimNames.RoleId` | 目前角色 ID |
| `rnm` | `JwtClaimNames.RoleName` | 目前角色名稱 |
| `sid` | `JwtClaimNames.SessionId` | 登入階段 GUID（對應 AUTHENTICATION_LOG） |
| `sys` | `JwtClaimNames.SystemCode` | 系統代碼：`PGM` 或 `DGPM` |
| `dpt` | `JwtClaimNames.Department` | 部門（可空） |
| `fac` | `JwtClaimNames.Factory` | 廠區（可空） |
| `sub` | 標準 | 同 `uid` |
| Name | `ClaimTypes.Name` | 顯示名稱 |

停用帳號／改角色後：AccessToken 最多約 10 分鐘內仍可能通過簽章驗證；DGPM 應於 API 呼叫失敗或定期 `GET /api/auth/me` 失敗時清 session。

## 2. Login

`POST /api/auth/login`（匿名）

### Request

```json
{
  "userId": "string",
  "password": "string",
  "roleId": "string|null",
  "systemCode": "DGPM"
}
```

- `systemCode`：消費端系統。DGPM 固定傳 `DGPM`；PGM Web 傳 `PGM` 或省略（預設 `PGM`）。
- 選單與可選角色會依 `systemCode` 過濾（`SET_FUNCTION.SYSTEM_CODE`／`DIM_ROLE.SYSTEM_CODE`）。
- **跨系統：**DGPM 只吃 PGM 核發的 DGPM 業務選單。Seed **`Admin`** 掛 `DGPMAdmin` → 業務模組全開（`RoleKPIList` 在 KPI 模組下）。帳號／角色 CRUD **只在 PGM**（直接開 PGM Web；`PgmAuthLink` 不掛 DGPMAdmin）。未授權 → 側欄空可接受。

### Success（`ApiResponse<LoginResponse>`，業務 code 依 PGM 慣例）

| 欄位 | 說明 |
|---|---|
| `accessToken` | JWT |
| `refreshToken` | 不落地；勿依賴 refresh |
| `expiresAt` | UTC 到期 |
| `passwordExpired` | `true`＝預設密碼，須強制改密 |
| `user` | `userId`／`userName`／`roleId`／`roleName`／`departmentCode`／`factoryNo`／`systemCode` |
| `menus` | 目前角色＋`systemCode` 之選單（含父層 M 自動帶出） |

### 錯誤碼（對外）

帳密錯誤**統一**以下一組，避免枚舉帳號是否存在：

| 情況 | `code`（字串） | `message`（建議） |
|---|---|---|
| 參數缺漏 | `200` | 必要參數缺漏或格式錯誤 |
| 帳號不存在／密碼錯／停用 | `AUTH_INVALID` | 帳號或密碼錯誤 |
| 無可用角色（該 systemCode 下） | `AUTH_NO_ROLE` | 尚未設定角色，請聯絡管理員 |
| 未授權（切角色等） | `400` | Unauthorized access |

> 實作注意：PGM 內部可區分找不到帳與密碼錯，但**回傳給 DGPM／瀏覽器必須走 `AUTH_INVALID`**。

## 3. 其他 Auth 端點

| 方法 | 路徑 | 說明 |
|---|---|---|
| POST | `/api/auth/logout` | Bearer；更新登入紀錄 |
| GET | `/api/auth/me` | Bearer；使用者資訊（含停用檢核） |
| GET | `/api/auth/menus` | Bearer；依 JWT `rid`＋`sys` 回選單 |
| POST | `/api/auth/switch-role` | Bearer；body含 `roleId`；重簽 JWT |
| POST | `/api/auth/change-password` | Bearer 或依現行強制改密流程 |
| GET | `/api/system/authentication-logs`（或現行 LoginHistory API） | 登入紀錄**只在 PGM**；DGPM 聯調可呼叫此 API 或開 PGM Web |

## 4. 選單父層規則

- 授權只管葉功能（`ACTION_TYPE=P`／畫面）。
- 若任一子項有授權，父模組（`ACTION_TYPE=M`）**自動出現**於 `menus`，即使未勾選父層。
- 無任何子項授權則不顯示該父層。

## 5. DGPM 部署參數（消費端）

```json
"Auth": {
  "AuthMode": "Local|PGM",
  "AllowPGMLoginEntry": true,
  "PgmBaseUrl": "http://localhost:9528",
  "SystemCode": "DGPM"
}
```

| AuthMode | AllowPGMLoginEntry | 行為 |
|---|---|---|
| Local | * | DGPM 本地 Auth（開發過渡） |
| PGM | true | DGPM 登入頁呼叫本契約 API |
| PGM | false | 拒絕由業務系統登入 |

PGM 不可達 → DGPM **不可完成登入**。
