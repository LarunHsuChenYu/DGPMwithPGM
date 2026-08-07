/* =============================================================================
   90_dev_seed_admin.sql
   DGPM_SPM — 本機／開發環境種子（可重複執行）

   ⚠ Local Auth 已退役：登入／帳號／角色／功能選單／登入歷程改由 PGM 主責。
   本檔仍種子 dbo.EMP_USER（如 AshtonHsu）供 KPI 資料權限等業務功能以 USER_ID 對照；
   密碼 hash 不再用於 DGPM 登入。
   - 前置依賴：先執行 10_dbo_qms_compat.sql（建立相容表）。
   - 可重複執行：角色／使用者／選單用 MERGE 或存在則更新；權限鏈每次重建。
   ============================================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @Now       DATETIME2(0) = SYSUTCDATETIME();
DECLARE @SeedUser  NVARCHAR(50) = N'SEED';
DECLARE @RoleId    NVARCHAR(50) = N'ADMIN';
DECLARE @UserId    NVARCHAR(50) = N'AshtonHsu';
/* BCrypt hash of the documented dev password (see docs/安裝文件.md). */
DECLARE @PwdHash   NVARCHAR(200) = N'$2a$11$VriSk5UglDGsOteJbtLcU.HmcTYTP2mU3/g.ub2DHppNsL2SJcXVa';

/* ---------- 1. 最高權限角色 ADMIN ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.DIM_ROLE WHERE ROLE_ID = @RoleId)
BEGIN
    INSERT INTO dbo.DIM_ROLE
        (ROLE_ID, ROLE_NAME, ROLE_TYPE, DEL_FLG, CRT_DATE, CRT_USER)
    VALUES
        (@RoleId, N'系統管理員', N'SYSTEM', 0, @Now, @SeedUser);
END
ELSE
BEGIN
    UPDATE dbo.DIM_ROLE
    SET ROLE_NAME = N'系統管理員',
        ROLE_TYPE = COALESCE(ROLE_TYPE, N'SYSTEM'),
        DEL_FLG   = 0,
        MDF_DATE  = @Now,
        MDF_USER  = @SeedUser
    WHERE ROLE_ID = @RoleId;
END;

/* ---------- 2. 開發帳號 AshtonHsu（啟用 + 重置為種子密碼 hash） ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.EMP_USER WHERE USER_ID = @UserId)
BEGIN
    INSERT INTO dbo.EMP_USER
        (USER_ID, USER_NAME, PASSWORD, TIT_NAME, EMAIL, TELEPHONE,
         FACTORY_NO, DPT_CODE, DEL_FLG, CRT_DATE, CRT_USER)
    VALUES
        (@UserId, N'Ashton Hsu', @PwdHash, N'Developer', NULL, NULL,
         NULL, NULL, 0, @Now, @SeedUser);
END
ELSE
BEGIN
    UPDATE dbo.EMP_USER
    SET USER_NAME = COALESCE(NULLIF(USER_NAME, N''), N'Ashton Hsu'),
        PASSWORD  = @PwdHash,
        DEL_FLG   = 0,
        MDF_DATE  = @Now,
        MDF_USER  = @SeedUser
    WHERE USER_ID = @UserId;
END;

/* ---------- 3. 指派角色 ---------- */
IF NOT EXISTS (
    SELECT 1 FROM dbo.MAP_USER_ROLE
    WHERE USER_ID = @UserId AND ROLE_ID = @RoleId)
BEGIN
    INSERT INTO dbo.MAP_USER_ROLE (USER_ID, ROLE_ID, CRT_DATE, CRT_USER)
    VALUES (@UserId, @RoleId, @Now, @SeedUser);
END;

/* ---------- 4. 最小功能選單（舊 SET_FUNCTION 後備；正式側欄改讀 SysFun） ----------
   若環境已有其他 FUNCTION 資料，MERGE 只補齊／更新下列代碼，不刪既有功能。
   FUNCTION_ID 為 DGPM 開發用代碼，非正式 QMS FunctionId。
   --------------------------------------------------------------------------- */
;WITH SeedFunctions AS
(
    SELECT * FROM (VALUES
        /* Parent modules */
        (N'BASIC',               N'基本資料管理',     NULL,                        NULL,              CAST(10 AS SMALLINT)),
        (N'SYSTEM',              N'系統權限管理',     NULL,                        NULL,              CAST(20 AS SMALLINT)),
        (N'PARAM',               N'系統參數管理',     NULL,                        NULL,              CAST(30 AS SMALLINT)),
        (N'KPI',                 N'經銷商KPI管理',    NULL,                        NULL,              CAST(40 AS SMALLINT)),
        (N'QUERY',               N'系統資料查詢',     NULL,                        NULL,              CAST(50 AS SMALLINT)),
        (N'DASHBOARD',           N'經銷商儀錶板',     NULL,                        NULL,              CAST(60 AS SMALLINT)),
        /* Children — URLs align with SysFun / Web routes */
        (N'BASIC_DEALERS',       N'經銷商資料維護',   N'/basic/dealers',           N'BASIC',          CAST(10 AS SMALLINT)),
        (N'BASIC_REGIONS',       N'區域資料維護',     N'/basic/regions',           N'BASIC',          CAST(20 AS SMALLINT)),
        (N'SYSTEM_KPI_PERMS',    N'KPI 權限維護',     N'/system/kpi-permissions',  N'SYSTEM',         CAST(30 AS SMALLINT)),
        (N'PARAM_EXCHANGE',      N'匯率參數維護',     N'/parameters/exchange-rates',N'PARAM',          CAST(10 AS SMALLINT)),
        (N'KPI_INDICATORS',      N'KPI 指標維護',     N'/kpi/indicators',          N'KPI',            CAST(10 AS SMALLINT)),
        (N'KPI_IMPORT',          N'KPI 資料匯入',     N'/kpi/import',              N'KPI',            CAST(20 AS SMALLINT)),
        (N'KPI_REVIEW',          N'KPI 資料審核',     N'/kpi/review',              N'KPI',            CAST(30 AS SMALLINT)),
        (N'QUERY_KPI_CHANGES',   N'KPI 異動查詢',     N'/query/kpi-changes',       N'QUERY',          CAST(10 AS SMALLINT)),
        (N'QUERY_IMPORT_LOGS',   N'匯入紀錄查詢',     N'/query/import-logs',       N'QUERY',          CAST(20 AS SMALLINT)),
        (N'DASHBOARD_MAIN',      N'經銷商儀錶板',     N'/dashboard',               N'DASHBOARD',      CAST(10 AS SMALLINT))
    ) AS V(FUNCTION_ID, FUNCTION_NAME, FUNCTION_URL, PARENT_ID, SORT_ID)
)
MERGE dbo.SET_FUNCTION AS T
USING SeedFunctions AS S
    ON T.FUNCTION_ID = S.FUNCTION_ID
WHEN MATCHED THEN
    UPDATE SET
        T.FUNCTION_NAME = S.FUNCTION_NAME,
        T.FUNCTION_URL  = S.FUNCTION_URL,
        T.PARENT_ID     = S.PARENT_ID,
        T.SORT_ID       = S.SORT_ID,
        T.DEL_FLG       = 0,
        T.MDF_DATE      = @Now,
        T.MDF_USER      = @SeedUser
WHEN NOT MATCHED THEN
    INSERT (FUNCTION_ID, FUNCTION_NAME, FUNCTION_URL, PARENT_ID, SORT_ID, DEL_FLG, CRT_DATE, CRT_USER)
    VALUES (S.FUNCTION_ID, S.FUNCTION_NAME, S.FUNCTION_URL, S.PARENT_ID, S.SORT_ID, 0, @Now, @SeedUser);

/* ---------- 4. 功能授權：改以 dbo.SysFun 為來源（選單已改讀 SysFun） ----------
   SET_FUNCTION 種子保留於歷史相容，但 ADMIN 權限鏈改綁 SysFun.Fun_ID。
   需先執行 15_dbo_sysfun.sql。
   --------------------------------------------------------------------------- */
DELETE FROM dbo.MAP_ROLE_RIGHT WHERE ROLE_ID = @RoleId;
DELETE FROM dbo.MAP_RIGHT_FUNCTION WHERE RIGHT_ID = @RoleId;

INSERT INTO dbo.MAP_ROLE_RIGHT (ROLE_ID, RIGHT_ID, CRT_DATE, CRT_USER)
VALUES (@RoleId, @RoleId, @Now, @SeedUser);

IF OBJECT_ID(N'dbo.SysFun', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.MAP_RIGHT_FUNCTION (RIGHT_ID, FUNCTION_ID, CRT_DATE, CRT_USER)
    SELECT @RoleId, F.Fun_ID, @Now, @SeedUser
    FROM dbo.SysFun AS F
    WHERE F.Del_YN = 'N';
END
ELSE
BEGIN
    /* 後備：若尚未建 SysFun，仍以 SET_FUNCTION 授權（僅過渡） */
    INSERT INTO dbo.MAP_RIGHT_FUNCTION (RIGHT_ID, FUNCTION_ID, CRT_DATE, CRT_USER)
    SELECT @RoleId, F.FUNCTION_ID, @Now, @SeedUser
    FROM dbo.SET_FUNCTION AS F
    WHERE F.DEL_FLG = 0;
END;

COMMIT TRANSACTION;
GO

/* ---------- Deprecated：PGM 主責舊 SET_FUNCTION 代碼（軟刪殘留，不 DROP 表） ---------- */
UPDATE dbo.SET_FUNCTION
SET DEL_FLG  = 1,
    MDF_DATE = SYSUTCDATETIME(),
    MDF_USER = N'SEED'
WHERE FUNCTION_ID IN (
        N'SYSTEM_FUNCTIONS',
        N'SYSTEM_ROLES',
        N'SYSTEM_USERS',
        N'QUERY_LOGIN_HISTORY')
  AND DEL_FLG = 0;
GO

PRINT N'90_dev_seed_admin.sql completed: EMP_USER/ADMIN seed for business (Local Auth retired).';
GO
