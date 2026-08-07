# Login 領域規則 — PGM

## 1. 基本說明
- 中文名稱：系統登入／角色切換／重設密碼
- 英文名稱：Login
- 主責系統：PGM（獨立權限平台；帳號／登入驗證主責＝唯 PGM）
- 資料寫入者：PGM Api（登入驗證／改密／JWT 簽發／AUTHENTICATION_LOG）
- 同步方向／頻率：DGPM（`AuthMode=PGM`）呼叫本平台 Auth API；不與 QMS 共用帳
- 主要識別鍵與對照鍵：`EMP_USER.USER_ID`；JWT claims：`uid`、`rid`、`rnm`、`sid`、`sys`
- 資料保留／封存年限：`CHANGE_PASSWORD_HISTORY` 依營運政策（未定則全留）
- 主要來源：`docs/PGM_Qlik_Login20260719.docx`；聯調定案 [`docs/contracts/pgm-dgpm-decisions.md`](../docs/contracts/pgm-dgpm-decisions.md)；契約 [`docs/contracts/auth-consumer-contract.md`](../docs/contracts/auth-consumer-contract.md)

## 2. 狀態機
| 狀態碼 | 狀態名稱 | 可轉移狀態 | 轉移動作 | 轉移條件 | 備註 |
|--------|----------|------------|----------|----------|------|
| ANON | 未登入 | AUTH／FORCE_PWD | 登入 | 帳密正確且該 `systemCode` 下有角色 | |
| FORCE_PWD | 須改密 | AUTH | 儲存新密碼 | 原密碼為預設 `0000`（hash 比對策略見實作）或標記為預設 | 改密後寫 `CHANGE_PASSWORD_HISTORY` |
| AUTH | 已登入 | ANON／AUTH | 登出／切換角色 | 有效 JWT；切角色不需重登 | 切角色後重載選單；角色須屬同一 `sys` |

## 3. 主責欄位清單
- `EMP_USER`：USER_ID、USER_NAME、PASSWORD（BCrypt）、EMAIL、TELEPHONE、DEL_FLG、CRT_*／MDF_*
- `CHANGE_PASSWORD_HISTORY`：LOG_ID、USER_ID、PASSWORD、LOG_DATE
- JWT claims：`uid`、`rid`、`rnm`、`sid`、`sys`（`PGM`｜`DGPM`）、可選 `dpt`／`fac`
- 欄位主責、可由何系統更新、衝突解決方式：全部由 PGM 寫入；禁止外部系統覆寫密碼／角色

## 4. 關鍵業務規則
1. 帳號＋密碼必填；查 `EMP_USER` 且 `DEL_FLG=0`（BMW bit），程式端 BCrypt `Verify`（**禁止** SQL 明文等值比對；**不採納** Login SRS 範例 SQL 之明文 `PASSWORD` 比對與 `DEL_FLG='N'`）
2. 無角色（該 `systemCode` 下 `MAP_USER_ROLE`∩`DIM_ROLE` 無資料）→ 拒登 `AUTH_NO_ROLE`
3. 帳號不存在／密碼錯／停用 → 對外統一 `AUTH_INVALID`（勿枚舉帳號是否存在）
4. 選單：`SET_FUNCTION` ⋈ `MAP_ROLE_FUNCTION`，依 `ROLE_ID`＋`SYSTEM_CODE` 過濾；父模組 M 在僅授權葉 P 時**自動帶出**（定案 Q7）
5. 預設密碼須強制改密；新密碼≠舊密碼；寫入 `CHANGE_PASSWORD_HISTORY`
6. 角色切換：更新 JWT 目前 ROLE_ID／`sys`，依該 ROLE_ID 重查選單，不需重新輸入帳密
7. AccessToken 約 **10 分鐘**；停用／改角色後開發期可接受 5～15 分內失效（定案 Q8）
8. 登入紀錄只寫 PGM；DGPM 查詢可呼叫 PGM API 或開 PGM Web（定案 Q9）

## 5. 外部整合
- 涉及系統：DGPM（業務系統；`AuthMode=Local|PGM`，`AllowPGMLoginEntry`）
- 主從關係與衝突解決規則：PGM 為帳密／JWT／選單主責；PGM 不可達則 DGPM 不可登入
- 介接方式、方向與觸發時機：DGPM → `POST /api/auth/login` 等（見 auth-consumer-contract）
- 契約版本、必要欄位與相容性期間：以 `docs/contracts/` 為準；Claim／錯誤碼禁止 DGPM 自創
- 冪等鍵／去重規則：改密寫歷程；登入本身非冪等寫入
- 失敗處理、重試上限、Dead Letter Queue 與人工補償：登入失敗回業務錯誤碼；無 DLQ
- 對帳方式、對帳頻率與責任人：N/A

## 6. 權限矩陣
| 角色 | 登入 | 切換角色 | 改密 | 備註 |
|------|------|----------|------|------|
| 任一有效角色（該 systemCode） | Y | 僅自己的角色列表（同 sys） | Y（自己） | |
| 無角色（該 systemCode） | N | — | — | `AUTH_NO_ROLE` |

## 7. 資料品質與稽核
- 外部匯入是否需 Staging？否
- 是否需保留完整異動記錄？改密寫 `CHANGE_PASSWORD_HISTORY`；登入寫 `AUTHENTICATION_LOG`
- 個資或敏感欄位：密碼僅 BCrypt hash；EMAIL／TELEPHONE 可空；日誌禁記明文密碼／Token
- 重送／重複匯入防護：N/A
- 資料修復或人工更正：僅系統管理員經 EMPSet／SQL；須稽核

## 8. 驗收與回復
- 必要測試：正確登入、錯密、無角色、強制改密、切角色後選單、JWT 過期、`systemCode` 過濾
- 聯調驗收清單：見 `pgm-dgpm-decisions.md` Q14
- 上線前置：SQL Seed 含角色／選單／`MAP_ROLE_FUNCTION`／`SYSTEM_CODE` → Api → Web／DGPM
- Feature Flag／相容期間：DGPM `AuthMode`／`AllowPGMLoginEntry`
- 回復：還原 Api／Web 部署；密碼／歷程誤寫需資料修復腳本（高風險，須授權）

## 9. 開放問題與決策紀錄

| 日期 | 問題／決策 | 決策者 | 影響範圍 | 追蹤項目 |
|---|---|---|---|---|
| 2026-08-05 | PGM↔DGPM 聯調定案（前提＋Q1～15）；JWT 規格共用 | 已確認 | Login／DGPM／契約 | `docs/contracts/pgm-dgpm-decisions.md` |
| 2026-08-03 | 預設密碼 `0000`：BCrypt Verify 判定 FORCE_PWD；新增帳號固定寫入該預設 hash | 已確認 | Login／EMPSet | |
| | BCrypt Work factor／套件是否與 QMS「同樣加密」強制一致 | 待定 | 密碼相容 | **未定案前各自獨立**；若未來共用再定 |
| 2026-07-31 | Login SRS 範例 SQL（明文 PASSWORD、`DEL_FLG='N'`）**不採納**；以 BMW bit＋BCrypt Verify 為準 | 已確認 | Login／實作／Agent 文件 | 憲法 §4 |
