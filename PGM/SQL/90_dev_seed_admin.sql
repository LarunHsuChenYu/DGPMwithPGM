/* =============================================================================
   90_dev_seed_admin.sql
   PGM — 開發環境種子（可重複執行）Phase 3

   前置：10_dbo_pgm_tables.sql、20_dbo_system_code.sql（本檔亦會自補 SYSTEM_CODE）
   角色：PGMAdmin／DGPMAdmin／DGPMUploader／DGPMReviewer
   測帳：Admin（PGMAdmin＋DGPMAdmin）；AshtonHsu 系統權限與 Admin 同等（同兩角色），
         另保留 DGPMUploader／DGPMReviewer 供聯調切角色。切 PgmUiMode＝掛 PGMAdmin（舊 ADMIN 亦認）。
   功能：PGM＝系統權限管理平台（AUTH*）；DGPM＝業務選單（對齊 SysFun 五大＋KPI）
   產品：Admin→DGPMAdmin→DGPM 業務選單（含 RoleKPIList 掛 KPIIndicator）；
         不含 PgmAuthLink（帳號／角色請直接開 PGM Web）；非管理員未授權→側欄空可接受。
   ============================================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

USE [PGM_DEV];
GO

IF OBJECT_ID(N'dbo.EMP_USER', N'U') IS NULL
    OR OBJECT_ID(N'dbo.DIM_ROLE', N'U') IS NULL
    OR OBJECT_ID(N'dbo.SET_FUNCTION', N'U') IS NULL
BEGIN
    DECLARE @WrongDbMsg NVARCHAR(400) =
        N'找不到 PGM 核心表。目前資料庫為 [' + DB_NAME() + N']。'
        + N'請先執行 10／20 腳本後再執行本種子。';
    RAISERROR(@WrongDbMsg, 16, 1);
END;
GO

/* 若略過 20 或 20 跑錯庫：在此補欄（須獨立批次，否則 MERGE 編譯期會 207） */
IF OBJECT_ID(N'dbo.DIM_ROLE', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.DIM_ROLE', N'SYSTEM_CODE') IS NULL
BEGIN
    ALTER TABLE dbo.DIM_ROLE ADD SYSTEM_CODE VARCHAR(20) NOT NULL
        CONSTRAINT DF_DIM_ROLE_SYSTEM_CODE DEFAULT ('PGM');
END
GO

IF OBJECT_ID(N'dbo.[SET_FUNCTION]', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.[SET_FUNCTION]', N'SYSTEM_CODE') IS NULL
BEGIN
    ALTER TABLE dbo.[SET_FUNCTION] ADD SYSTEM_CODE VARCHAR(20) NOT NULL
        CONSTRAINT DF_SET_FUNCTION_SYSTEM_CODE DEFAULT ('PGM');
END
GO

IF OBJECT_ID(N'dbo.DIM_ROLE', N'U') IS NULL
   OR OBJECT_ID(N'dbo.[SET_FUNCTION]', N'U') IS NULL
   OR COL_LENGTH(N'dbo.DIM_ROLE', N'SYSTEM_CODE') IS NULL
   OR COL_LENGTH(N'dbo.[SET_FUNCTION]', N'SYSTEM_CODE') IS NULL
BEGIN
    DECLARE @SysCodeMsg NVARCHAR(400) =
        N'缺少 SYSTEM_CODE 欄位（目前庫 [' + DB_NAME() + N']）。'
        + N'請確認已選 PGM_DEV 並執行 20_dbo_system_code.sql。';
    RAISERROR(@SysCodeMsg, 16, 1);
END
GO

BEGIN TRANSACTION;

DECLARE @Now      DATETIME     = GETDATE();
DECLARE @SeedUser NVARCHAR(10) = N'SEED';
DECLARE @PwdHash  VARCHAR(500) = N'$2a$11$VriSk5UglDGsOteJbtLcU.HmcTYTP2mU3/g.ub2DHppNsL2SJcXVa';

/* ---------- 1. 角色（含 SYSTEM_CODE）；舊 ADMIN／USER／VIEWER 軟刪 ---------- */
;WITH SeedRoles AS
(
    SELECT * FROM (VALUES
        (N'PGMAdmin',      N'PGM管理者',       N'PGM'),
        (N'DGPMAdmin',     N'DGPM管理者',      N'DGPM'),
        (N'DGPMUploader',  N'DGPM KPI上傳',    N'DGPM'),
        (N'DGPMReviewer',  N'DGPM KPI覆核',    N'DGPM')
    ) AS V(ROLE_ID, ROLE_NAME, SYSTEM_CODE)
)
MERGE dbo.DIM_ROLE AS T
USING SeedRoles AS S
    ON T.ROLE_ID = S.ROLE_ID
WHEN MATCHED THEN
    UPDATE SET
        T.ROLE_NAME    = S.ROLE_NAME,
        T.SYSTEM_CODE  = S.SYSTEM_CODE,
        T.DEL_FLG      = 0,
        T.MDF_DATE     = @Now,
        T.MDF_USER     = @SeedUser
WHEN NOT MATCHED THEN
    INSERT (ROLE_ID, ROLE_NAME, SYSTEM_CODE, DEL_FLG, CRT_DATE, CRT_USER)
    VALUES (S.ROLE_ID, S.ROLE_NAME, S.SYSTEM_CODE, 0, @Now, @SeedUser);

UPDATE dbo.DIM_ROLE
SET DEL_FLG = 1, MDF_DATE = @Now, MDF_USER = @SeedUser
WHERE ROLE_ID IN (N'ADMIN', N'USER', N'VIEWER') AND DEL_FLG = 0;

/* ---------- 2. 開發帳號（AshtonHsu＋Admin） ----------
   系統權限能力：兩者皆掛 PGMAdmin＋DGPMAdmin（可切 PgmUiMode、可操作 AUTH）。
   Login 依 systemCode 濾角色，故須另掛 DGPMAdmin（不可只靠 PGMAdmin／舊 ADMIN）。
   AshtonHsu 額外保留 Uploader／Reviewer；重跑時先刪再插（idempotent）。
   --------------------------------------------------------------------------- */
;WITH SeedUsers AS
(
    SELECT * FROM (VALUES
        (N'AshtonHsu', N'Ashton Hsu'),
        (N'Admin',     N'系統管理員')
    ) AS V(USER_ID, USER_NAME)
)
MERGE dbo.EMP_USER AS T
USING SeedUsers AS S
    ON T.USER_ID = S.USER_ID
WHEN MATCHED THEN
    UPDATE SET
        T.USER_NAME = S.USER_NAME,
        T.PASSWORD  = @PwdHash,
        T.DEL_FLG   = 0,
        T.MDF_DATE  = @Now,
        T.MDF_USER  = @SeedUser
WHEN NOT MATCHED THEN
    INSERT (USER_ID, USER_NAME, PASSWORD, EMAIL, TELEPHONE, DEL_FLG, CRT_DATE, CRT_USER)
    VALUES (S.USER_ID, S.USER_NAME, @PwdHash, NULL, NULL, 0, @Now, @SeedUser);

DELETE FROM dbo.MAP_USER_ROLE
WHERE USER_ID IN (N'AshtonHsu', N'Admin');

/* Admin＝系統管理員：PGMAdmin＋DGPMAdmin（可切 Mode、進 DGPM 全業務選單） */
INSERT INTO dbo.MAP_USER_ROLE (USER_ID, ROLE_ID, CRT_DATE, CRT_USER)
VALUES
    (N'Admin', N'PGMAdmin',  @Now, @SeedUser),
    (N'Admin', N'DGPMAdmin', @Now, @SeedUser);

/* AshtonHsu：系統權限同 Admin（PGMAdmin＋DGPMAdmin），另保留 Uploader／Reviewer */
INSERT INTO dbo.MAP_USER_ROLE (USER_ID, ROLE_ID, CRT_DATE, CRT_USER)
VALUES
    (N'AshtonHsu', N'PGMAdmin',      @Now, @SeedUser),
    (N'AshtonHsu', N'DGPMAdmin',     @Now, @SeedUser),
    (N'AshtonHsu', N'DGPMUploader',  @Now, @SeedUser),
    (N'AshtonHsu', N'DGPMReviewer',  @Now, @SeedUser);

/* 舊 ADMIN 指派清掉（該角色僅 PGM Fun，無法滿足 DGPM 系統管理員預期） */
DELETE FROM dbo.MAP_USER_ROLE
WHERE ROLE_ID = N'ADMIN';

/* ---------- 3. SET_FUNCTION：PGM 管理＋DGPM 業務（Fun_ID／Url 對齊 DGPM SysFun） ---------- */
;WITH Seed AS
(
    SELECT * FROM (VALUES
        /* PGM 平台管理（扁平葉功能；父層名僅顯示） */
        (N'AUTH01', N'帳號維護',       N'/system/users',             CAST(10 AS SMALLINT),
         N'P', N'Y', N'Y', N'帳號與角色指派', N'系統管理', CAST(NULL AS VARCHAR(20)), N'PGM'),
        (N'AUTH02', N'角色權限設定',   N'/system/roles',             CAST(20 AS SMALLINT),
         N'P', N'Y', N'Y', N'角色×功能授權', N'系統管理', NULL, N'PGM'),
        (N'AUTH03', N'重設密碼',       N'/account/change-password',  CAST(30 AS SMALLINT),
         N'P', N'Y', N'Y', N'變更密碼', N'系統管理', NULL, N'PGM'),
        (N'AUTH04', N'系統代碼維護',   N'/parameters/param-set',      CAST(40 AS SMALLINT),
         N'P', N'Y', N'Y', N'SET_PARAM 維護', N'系統管理', NULL, N'PGM'),
        (N'AUTH05', N'系統報表',       N'/reports',                  CAST(50 AS SMALLINT),
         N'P', N'Y', N'Y', N'系統報表佔位', N'系統管理', NULL, N'PGM'),
        (N'AUTH06', N'功能維護',       N'/Permission/FunctionList',  CAST(60 AS SMALLINT),
         N'P', N'Y', N'Y', N'SET_FUNCTION 維護', N'系統管理', NULL, N'PGM'),
        (N'AUTH07', N'角色主檔',       N'/system/role-master',       CAST(70 AS SMALLINT),
         N'P', N'Y', N'Y', N'DIM_ROLE 簡易維護', N'系統管理', NULL, N'PGM'),
        (N'AUTH08', N'登入紀錄',       N'/query/login-history',      CAST(80 AS SMALLINT),
         N'P', N'Y', N'Y', N'AUTHENTICATION_LOG', N'系統管理', NULL, N'PGM'),
        (N'AUTH09', N'代重設密碼',     N'/system/users',             CAST(85 AS SMALLINT),
         N'B', N'N', N'Y', N'管理員代他人重設密碼（預設0000）', N'系統管理', NULL, N'PGM'),

        /* DGPM 業務：六大模組 M + 葉 P（FUNCTION_ID／FUNCTION_URL 對齊 DGPM_SPM SQL/15_dbo_sysfun.sql） */
        /* 1 Masterdata */
        (N'Masterdata', N'基本資料管理', NULL, CAST(100 AS SMALLINT),
         N'M', N'Y', N'Y', N'基本資料管理模組', N'基本資料管理', NULL, N'DGPM'),
        (N'DealerList', N'經銷商設定管理', N'/basic/dealers', CAST(110 AS SMALLINT),
         N'P', N'Y', N'Y', N'增刪修經銷商', N'基本資料管理', N'Masterdata', N'DGPM'),
        (N'OrgList', N'區域組織管理', N'/basic/regions', CAST(120 AS SMALLINT),
         N'P', N'Y', N'Y', N'增刪修區域組織', N'基本資料管理', N'Masterdata', N'DGPM'),
        /* 2 SysConfig（原 Permission／PgmAuthLink 已退役：帳號角色請直接開 PGM Web） */
        (N'SysConfig', N'系統參數管理', NULL, CAST(300 AS SMALLINT),
         N'M', N'Y', N'Y', N'系統參數管理模組', N'系統參數管理', NULL, N'DGPM'),
        (N'ExchangeRates', N'匯率參數設定', N'/parameters/exchange-rates', CAST(310 AS SMALLINT),
         N'P', N'Y', N'Y', N'匯率參數維護', N'系統參數管理', N'SysConfig', N'DGPM'),
        /* 3 KPIIndicator（含 RoleKPIList＝KPI 資料範圍） */
        (N'KPIIndicator', N'經銷商KPI管理', NULL, CAST(400 AS SMALLINT),
         N'M', N'Y', N'Y', N'經銷商 KPI 管理模組', N'經銷商KPI管理', NULL, N'DGPM'),
        (N'KPIManage', N'KPI 指標設定', N'/kpi/indicators', CAST(410 AS SMALLINT),
         N'P', N'Y', N'Y', N'KPI 指標維護', N'經銷商KPI管理', N'KPIIndicator', N'DGPM'),
        (N'KPIImport', N'KPI 數據匯入', N'/kpi/import', CAST(420 AS SMALLINT),
         N'P', N'Y', N'Y', N'KPI Excel 上傳／匯入預覽', N'經銷商KPI管理', N'KPIIndicator', N'DGPM'),
        (N'KPIImpReview', N'KPI 數據覆核與解鎖', N'/kpi/review', CAST(430 AS SMALLINT),
         N'P', N'Y', N'Y', N'KPI 覆核與解鎖', N'經銷商KPI管理', N'KPIIndicator', N'DGPM'),
        (N'RoleKPIList', N'KPI 資料權限設定', N'/system/kpi-permissions', CAST(440 AS SMALLINT),
         N'P', N'Y', N'Y', N'KPI 資料範圍權限', N'經銷商KPI管理', N'KPIIndicator', N'DGPM'),
        /*
          系統管理權限父層（DGPM）：
          FUNCTION_ID 全域唯一，無法另種 AUTH01＋SYSTEM_CODE=DGPM。
          Mode=Off 時 AuthService 將 MAP 到的 AUTH* 掛到此父層並改寫 URL。
          種子保留父層定義（IS_MENU 可由 Mode 邏輯虛擬注入；此列作對照／手動授權備援）。
         */
        (N'Permission', N'系統管理權限', NULL, CAST(200 AS SMALLINT),
         N'M', N'N', N'Y', N'PgmUiMode=Off 時由 AuthService 注入選單', N'系統管理權限', NULL, N'DGPM'),
        (N'PgmAuthLink', N'帳號與角色維護', N'ext:pgm', CAST(220 AS SMALLINT),
         N'P', N'N', N'N', N'保留定義；不掛 DGPMAdmin', N'系統管理權限', N'Permission', N'DGPM'),
        /* 5 Syslog */
        (N'Syslog', N'系統資料查詢', NULL, CAST(500 AS SMALLINT),
         N'M', N'Y', N'Y', N'系統資料查詢模組', N'系統資料查詢', NULL, N'DGPM'),
        (N'KPIChgLog', N'KPI 異動紀錄查詢', N'/query/kpi-changes', CAST(510 AS SMALLINT),
         N'P', N'Y', N'Y', N'KPI 異動紀錄', N'系統資料查詢', N'Syslog', N'DGPM'),
        (N'KPIImpLog', N'KPI 匯入日誌查詢', N'/query/import-logs', CAST(520 AS SMALLINT),
         N'P', N'Y', N'Y', N'KPI 匯入紀錄', N'系統資料查詢', N'Syslog', N'DGPM'),
        /* 6 Dashboard */
        (N'Dashboard', N'經銷商儀錶板', NULL, CAST(600 AS SMALLINT),
         N'M', N'Y', N'Y', N'經銷商儀錶板模組', N'經銷商儀錶板', NULL, N'DGPM'),
        (N'RdtQlik', N'Qlik Cloud', N'/dashboard', CAST(610 AS SMALLINT),
         N'P', N'Y', N'Y', N'Qlik Cloud 儀錶板', N'經銷商儀錶板', N'Dashboard', N'DGPM')
    ) AS V(
        FUNCTION_ID, FUNCTION_NAME, FUNCTION_URL, SORT_ID,
        ACTION_TYPE, IS_MENU, IS_ENABLED, FUN_DESC, PARENT_NAME, PARENT_ID, SYSTEM_CODE)
)
MERGE dbo.[SET_FUNCTION] AS T
USING Seed AS S
    ON T.FUNCTION_ID = S.FUNCTION_ID
WHEN MATCHED THEN
    UPDATE SET
        T.FUNCTION_NAME = S.FUNCTION_NAME,
        T.FUNCTION_URL  = S.FUNCTION_URL,
        T.PARENT_ID     = S.PARENT_ID,
        T.PARENT_NAME   = S.PARENT_NAME,
        T.SORT_ID       = S.SORT_ID,
        T.ACTION_TYPE   = S.ACTION_TYPE,
        T.IS_MENU       = S.IS_MENU,
        T.IS_ENABLED    = S.IS_ENABLED,
        T.FUN_DESC      = S.FUN_DESC,
        T.SYSTEM_CODE   = S.SYSTEM_CODE,
        T.DEL_FLG       = 0,
        T.MDF_DATE      = @Now,
        T.MDF_USER      = @SeedUser
WHEN NOT MATCHED THEN
    INSERT
    (
        FUNCTION_ID, FUNCTION_NAME, FUNCTION_URL, PARENT_NAME, SORT_ID, DEL_FLG,
        CRT_DATE, CRT_USER,
        PARENT_ID, ACTION_TYPE, IS_MENU, IS_ENABLED, FUN_DESC, ICON, SYSTEM_CODE
    )
    VALUES
    (
        S.FUNCTION_ID, S.FUNCTION_NAME, S.FUNCTION_URL, S.PARENT_NAME, S.SORT_ID, 0,
        @Now, @SeedUser,
        S.PARENT_ID, S.ACTION_TYPE, S.IS_MENU, S.IS_ENABLED, S.FUN_DESC, NULL, S.SYSTEM_CODE
    );

/* 舊英文 ID／已退役 DGPM 頁（帳號／角色／功能清單／登入軌跡改由 PGM AUTH*） */
UPDATE dbo.[SET_FUNCTION]
SET DEL_FLG = 1, IS_MENU = N'N', IS_ENABLED = N'N', MDF_DATE = @Now, MDF_USER = @SeedUser
WHERE DEL_FLG = 0
  AND FUNCTION_ID IN (
      N'SysMgmt', N'RoleFunList', N'Accounts', N'ResetPwd', N'ParamSet', N'SysReport',
      N'FunctionList', N'LoginHistory', N'KPIAccLog'
  );

/* Permission：父層保留（Mode=Off 由 API 注入選單）；PgmAuthLink 仍軟刪 */
UPDATE dbo.[SET_FUNCTION]
SET DEL_FLG = 1, IS_MENU = N'N', IS_ENABLED = N'N', MDF_DATE = @Now, MDF_USER = @SeedUser
WHERE FUNCTION_ID = N'PgmAuthLink'
  AND SYSTEM_CODE = N'DGPM';

UPDATE dbo.[SET_FUNCTION]
SET DEL_FLG = 0, IS_MENU = N'N', IS_ENABLED = N'Y',
    FUNCTION_NAME = N'系統管理權限', PARENT_NAME = N'系統管理權限',
    MDF_DATE = @Now, MDF_USER = @SeedUser
WHERE FUNCTION_ID = N'Permission'
  AND SYSTEM_CODE = N'DGPM';

/* ---------- 4. MAP_ROLE_FUNCTION ---------- */
DELETE FROM dbo.MAP_ROLE_FUNCTION
WHERE ROLE_ID IN (N'ADMIN', N'USER', N'VIEWER', N'PGMAdmin', N'DGPMAdmin', N'DGPMUploader', N'DGPMReviewer');

/* 保險：任何角色皆不再掛 PgmAuthLink（帳號／角色維護請直接開 PGM Web） */
DELETE FROM dbo.MAP_ROLE_FUNCTION
WHERE FUNCTION_ID = N'PgmAuthLink';

/* PGMAdmin：所有 PGM 系統功能（含管理頁／AUTH09 按鈕；不含 DGPM 業務） */
INSERT INTO dbo.MAP_ROLE_FUNCTION (ROLE_ID, FUNCTION_ID, CRT_DATE, CRT_USER)
SELECT N'PGMAdmin', F.FUNCTION_ID, @Now, @SeedUser
FROM dbo.[SET_FUNCTION] AS F
WHERE F.DEL_FLG = 0 AND F.IS_ENABLED = 'Y' AND F.SYSTEM_CODE = N'PGM'
  AND F.ACTION_TYPE IN (N'P', N'B');

/* DGPMAdmin：全部啟用中的 DGPM 葉（不含 PgmAuthLink）＋ AUTH 系統權限（Mode=Off 時可維護） */
INSERT INTO dbo.MAP_ROLE_FUNCTION (ROLE_ID, FUNCTION_ID, CRT_DATE, CRT_USER)
SELECT N'DGPMAdmin', F.FUNCTION_ID, @Now, @SeedUser
FROM dbo.[SET_FUNCTION] AS F
WHERE F.DEL_FLG = 0 AND F.IS_ENABLED = 'Y' AND F.SYSTEM_CODE = N'DGPM'
  AND F.ACTION_TYPE = N'P'
  AND F.FUNCTION_ID <> N'PgmAuthLink';

INSERT INTO dbo.MAP_ROLE_FUNCTION (ROLE_ID, FUNCTION_ID, CRT_DATE, CRT_USER)
SELECT N'DGPMAdmin', F.FUNCTION_ID, @Now, @SeedUser
FROM dbo.[SET_FUNCTION] AS F
WHERE F.DEL_FLG = 0 AND F.IS_ENABLED = 'Y' AND F.SYSTEM_CODE = N'PGM'
  AND F.FUNCTION_ID IN (
      N'AUTH01', N'AUTH02', N'AUTH03', N'AUTH04',
      N'AUTH06', N'AUTH07', N'AUTH08', N'AUTH09')
  AND NOT EXISTS (
      SELECT 1 FROM dbo.MAP_ROLE_FUNCTION M
      WHERE M.ROLE_ID = N'DGPMAdmin' AND M.FUNCTION_ID = F.FUNCTION_ID);

/* DGPMUploader：指標／匯入／匯入日誌／異動紀錄（唯讀查詢） */
INSERT INTO dbo.MAP_ROLE_FUNCTION (ROLE_ID, FUNCTION_ID, CRT_DATE, CRT_USER)
SELECT N'DGPMUploader', F.FUNCTION_ID, @Now, @SeedUser
FROM dbo.[SET_FUNCTION] AS F
WHERE F.FUNCTION_ID IN (N'KPIManage', N'KPIImport', N'KPIImpLog', N'KPIChgLog')
  AND F.DEL_FLG = 0;

/* DGPMReviewer：覆核／匯入日誌／異動紀錄 */
INSERT INTO dbo.MAP_ROLE_FUNCTION (ROLE_ID, FUNCTION_ID, CRT_DATE, CRT_USER)
SELECT N'DGPMReviewer', F.FUNCTION_ID, @Now, @SeedUser
FROM dbo.[SET_FUNCTION] AS F
WHERE F.FUNCTION_ID IN (N'KPIImpReview', N'KPIImpLog', N'KPIChgLog')
  AND F.DEL_FLG = 0;

/* ---------- 5. Param 示範＋PgmUiMode（系統權限 UI 所在端） ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.[SET_PARAMITEM] WHERE SET_ITEM = N'A001')
BEGIN
    INSERT INTO dbo.[SET_PARAMITEM]
        (SET_ITEM, SET_ITEM_NAME, MEMO, DEL_FLG, CRT_DATE, CRT_USER)
    VALUES
        (N'A001', N'功能代碼', N'與 SET_FUNCTION.FUNCTION_ID 對照', 0, @Now, @SeedUser);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.[SET_PARAMITEM] WHERE SET_ITEM = N'Auth')
BEGIN
    INSERT INTO dbo.[SET_PARAMITEM]
        (SET_ITEM, SET_ITEM_NAME, MEMO, DEL_FLG, CRT_DATE, CRT_USER)
    VALUES
        (N'Auth', N'授權設定', N'含 PgmUiMode（On＝PGM／Off＝DGPM）', 0, @Now, @SeedUser);
END;

;WITH P AS
(
    SELECT * FROM (VALUES
        (N'A001', N'AUTH01', N'帳號維護', 1),
        (N'A001', N'AUTH02', N'角色權限設定', 2),
        (N'A001', N'AUTH06', N'功能維護', 6),
        (N'A001', N'AUTH07', N'角色主檔', 7),
        (N'A001', N'KPIImport', N'KPI數據匯入', 20),
        (N'A001', N'KPIImpReview', N'KPI覆核解鎖', 21),
        (N'Auth', N'PgmUiMode', N'On', 1)
    ) AS V(SET_ITEM, SET_ID, SET_VALUE, SORT_ORDER)
)
MERGE dbo.[SET_PARAM] AS T
USING P AS S ON T.SET_ITEM = S.SET_ITEM AND T.SET_ID = S.SET_ID
WHEN MATCHED THEN
    UPDATE SET
               /* PgmUiMode：已存在時不覆寫營運值，僅確保未刪 */
               T.SET_VALUE = CASE
                   WHEN S.SET_ITEM = N'Auth' AND S.SET_ID = N'PgmUiMode' THEN T.SET_VALUE
                   ELSE S.SET_VALUE END,
               T.SORT_ORDER = S.SORT_ORDER, T.DEL_FLG = 0,
               T.MDF_DATE = @Now, T.MDF_USER = @SeedUser
WHEN NOT MATCHED THEN
    INSERT (SET_ITEM, SET_ID, SET_VALUE, SORT_ORDER, MEMO, DEL_FLG, CRT_DATE, CRT_USER)
    VALUES (S.SET_ITEM, S.SET_ID, S.SET_VALUE, S.SORT_ORDER,
            CASE WHEN S.SET_ITEM = N'Auth' AND S.SET_ID = N'PgmUiMode'
                 THEN N'On＝系統權限 UI 在 PGM；Off＝在 DGPM'
                 ELSE N'' END,
            0, @Now, @SeedUser);

COMMIT TRANSACTION;
PRINT N'90_dev_seed_admin.sql completed (Phase 3：AshtonHsu／Admin＋PGMAdmin／DGPMAdmin＋DGPM menus).';
GO
