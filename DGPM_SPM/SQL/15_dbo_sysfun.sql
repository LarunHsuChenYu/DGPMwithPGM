/* =============================================================================
   15_dbo_sysfun.sql
   DGPM_SPM — 正式表 dbo.SysFun（系統功能設定檔，依 DGPM_TableList）

   - 性質：TableList 正式規格（非 provisional draft）
   - Idempotent：表不存在才建立；種子以 MERGE 可重複執行
   - 頂層 Parent_ID：本專案約定僅 NULL（Action_Type='M' 亦為 NULL）
     TableList 曾寫「0 or NULL」，已定案不用 0；種子與寫入皆不得填 '0'
   - Icon：欄位保留，種子不維護（暫不使用）
   ============================================================================= */

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.SysFun', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SysFun
    (
        Fun_ID      VARCHAR(20)    NOT NULL,
        Fun_Name    NVARCHAR(50)   NOT NULL,
        Parent_ID   VARCHAR(20)    NULL,
        Action_Type CHAR(1)        NOT NULL,
        Url_Path    NVARCHAR(50)   NULL,
        Icon        NVARCHAR(50)   NULL,
        Sort_Order  DECIMAL(6, 2)  NOT NULL,
        Is_Menu     CHAR(1)        NOT NULL CONSTRAINT DF_SysFun_Is_Menu DEFAULT ('N'),
        Is_Enabled  CHAR(1)        NOT NULL CONSTRAINT DF_SysFun_Is_Enabled DEFAULT ('N'),
        Fun_Desc    NVARCHAR(500)  NULL,
        Del_YN      CHAR(1)        NOT NULL CONSTRAINT DF_SysFun_Del_YN DEFAULT ('N'),
        Cre_Person  NVARCHAR(50)   NOT NULL,
        Cre_Date    DATETIME       NOT NULL,
        Chg_Person  NVARCHAR(50)   NOT NULL,
        Chg_Date    DATETIME       NOT NULL,
        CONSTRAINT PK_SysFun PRIMARY KEY CLUSTERED (Fun_ID),
        CONSTRAINT CK_SysFun_Action_Type CHECK (Action_Type IN ('M', 'P', 'B')),
        CONSTRAINT CK_SysFun_Is_Menu CHECK (Is_Menu IN ('Y', 'N')),
        CONSTRAINT CK_SysFun_Is_Enabled CHECK (Is_Enabled IN ('Y', 'N')),
        CONSTRAINT CK_SysFun_Del_YN CHECK (Del_YN IN ('Y', 'N'))
    );

    CREATE NONCLUSTERED INDEX IX_SysFun_Parent_Sort
        ON dbo.SysFun (Parent_ID, Sort_Order, Fun_ID)
        WHERE Del_YN = 'N';

    CREATE NONCLUSTERED INDEX IX_SysFun_Menu
        ON dbo.SysFun (Sort_Order, Fun_ID)
        WHERE Del_YN = 'N' AND Is_Menu = 'Y' AND Is_Enabled = 'Y';
END;
GO

/* ---------- 種子：六大模組 M + 14 葉功能 P（正式命名） ---------- */
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @Now      DATETIME     = GETDATE();
DECLARE @SeedUser NVARCHAR(50) = N'SEED';

;WITH Seed AS
(
    SELECT * FROM (VALUES
        /* 1 Masterdata */
        (N'Masterdata',    N'基本資料管理',     NULL,           N'M', NULL,                              CAST(1.00 AS DECIMAL(6,2)), N'Y', N'Y', N'基本資料管理模組'),
        (N'DealerList',    N'經銷商設定管理',   N'Masterdata',  N'P', N'/basic/dealers',                 CAST(1.10 AS DECIMAL(6,2)), N'Y', N'Y', N'增刪修經銷商'),
        (N'OrgList',       N'區域組織管理',     N'Masterdata',  N'P', N'/basic/regions',                 CAST(1.20 AS DECIMAL(6,2)), N'Y', N'Y', N'增刪修區域組織'),
        /* 2 SysConfig（Permission／PgmAuthLink 已退役；選單真相在 PGM SET_FUNCTION） */
        (N'SysConfig',     N'系統參數管理',     NULL,           N'M', NULL,                              CAST(3.00 AS DECIMAL(6,2)), N'Y', N'Y', N'系統參數管理模組'),
        (N'ExchangeRates', N'匯率參數設定',     N'SysConfig',   N'P', N'/parameters/exchange-rates',      CAST(3.10 AS DECIMAL(6,2)), N'Y', N'Y', N'匯率參數維護'),
        /* 3 KPIIndicator（含 RoleKPIList＝KPI 資料範圍） */
        (N'KPIIndicator',  N'經銷商KPI管理',    NULL,           N'M', NULL,                              CAST(4.00 AS DECIMAL(6,2)), N'Y', N'Y', N'經銷商 KPI 管理模組'),
        (N'KPIManage',     N'KPI 指標設定',     N'KPIIndicator',N'P', N'/kpi/indicators',                CAST(4.10 AS DECIMAL(6,2)), N'Y', N'Y', N'KPI 指標維護'),
        (N'KPIImport',     N'KPI 數據匯入',     N'KPIIndicator',N'P', N'/kpi/import',                    CAST(4.20 AS DECIMAL(6,2)), N'Y', N'Y', N'KPI 數據匯入'),
        (N'KPIImpReview',  N'KPI 數據覆核與解鎖',N'KPIIndicator',N'P', N'/kpi/review',                   CAST(4.30 AS DECIMAL(6,2)), N'Y', N'Y', N'KPI 覆核與解鎖'),
        (N'RoleKPIList',   N'KPI 資料權限設定', N'KPIIndicator',N'P', N'/system/kpi-permissions',        CAST(4.40 AS DECIMAL(6,2)), N'Y', N'Y', N'KPI 資料範圍權限'),
        /* 5 Syslog（登入軌跡查詢已改由 PGM 主責） */
        (N'Syslog',        N'系統資料查詢',     NULL,           N'M', NULL,                              CAST(5.00 AS DECIMAL(6,2)), N'Y', N'Y', N'系統資料查詢模組'),
        (N'KPIChgLog',     N'KPI 異動紀錄查詢', N'Syslog',      N'P', N'/query/kpi-changes',             CAST(5.10 AS DECIMAL(6,2)), N'Y', N'Y', N'KPI 異動紀錄'),
        (N'KPIImpLog',     N'KPI 匯入日誌查詢', N'Syslog',      N'P', N'/query/import-logs',             CAST(5.20 AS DECIMAL(6,2)), N'Y', N'Y', N'KPI 匯入日誌'),
        /* 6 Dashboard */
        (N'Dashboard',     N'經銷商儀錶板',     NULL,           N'M', NULL,                              CAST(6.00 AS DECIMAL(6,2)), N'Y', N'Y', N'經銷商儀錶板模組'),
        (N'RdtQlik',       N'Qlik Cloud',       N'Dashboard',   N'P', N'/dashboard',                     CAST(6.10 AS DECIMAL(6,2)), N'Y', N'Y', N'Qlik Cloud 儀錶板')
    ) AS V(Fun_ID, Fun_Name, Parent_ID, Action_Type, Url_Path, Sort_Order, Is_Menu, Is_Enabled, Fun_Desc)
)
MERGE dbo.SysFun AS T
USING Seed AS S
    ON T.Fun_ID = S.Fun_ID
WHEN MATCHED THEN
    UPDATE SET
        T.Fun_Name    = S.Fun_Name,
        T.Parent_ID   = S.Parent_ID,
        T.Action_Type = S.Action_Type,
        T.Url_Path    = S.Url_Path,
        T.Sort_Order  = S.Sort_Order,
        T.Is_Menu     = S.Is_Menu,
        T.Is_Enabled  = S.Is_Enabled,
        T.Fun_Desc    = S.Fun_Desc,
        T.Del_YN      = 'N',
        T.Chg_Person  = @SeedUser,
        T.Chg_Date    = @Now
WHEN NOT MATCHED THEN
    INSERT
    (
        Fun_ID, Fun_Name, Parent_ID, Action_Type, Url_Path, Icon,
        Sort_Order, Is_Menu, Is_Enabled, Fun_Desc, Del_YN,
        Cre_Person, Cre_Date, Chg_Person, Chg_Date
    )
    VALUES
    (
        S.Fun_ID, S.Fun_Name, S.Parent_ID, S.Action_Type, S.Url_Path, NULL,
        S.Sort_Order, S.Is_Menu, S.Is_Enabled, S.Fun_Desc, 'N',
        @SeedUser, @Now, @SeedUser, @Now
    );

COMMIT TRANSACTION;
GO

/* ---------- Deprecated：PGM 主責功能（軟刪既有環境殘留種子，不 DROP 表） ----------
   FunctionList / RoleFunList / Accounts / KPIAccLog 已退役；選單真相改以 PGM 為準。
   Permission / PgmAuthLink：DGPM 側欄不再顯示「系統權限管理」；帳號角色請直接開 PGM Web。
   --------------------------------------------------------------------------- */
UPDATE dbo.SysFun
SET Del_YN     = 'Y',
    Is_Menu    = 'N',
    Is_Enabled = 'N',
    Chg_Person = N'SEED',
    Chg_Date   = GETDATE()
WHERE Fun_ID IN (N'FunctionList', N'RoleFunList', N'Accounts', N'KPIAccLog', N'Permission', N'PgmAuthLink')
  AND Del_YN = 'N';
GO

PRINT N'15_dbo_sysfun.sql completed: dbo.SysFun business seed (PGM-owned menus soft-deleted; RoleKPIList under KPIIndicator).';
GO
