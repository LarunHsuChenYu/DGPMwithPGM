/* =============================================================================
   10_dbo_qms_compat.sql
   DGPM_SPM — 命名相容表（欄位形狀相容舊 QMS 範例；非本專案正式規格）

   ⚠ 重要邊界說明：
   - 表名／欄位命名相容舊 QMS，方便沿用既有 Repository SQL；本機開發時這些表
     建在「本專案資料庫」，搭配 90_dev_seed_admin.sql 即可，不必連正式 QMS。
   - 既有 QMS DB「沒有」正式 DDL 文件；此檔僅依 Infrastructure/Repositories
     實際引用到的欄位「反推」出最小相容定義，欄位型別與長度為推測值，
     僅供本機/全新開發環境建表使用。
   - 不得以此檔作為既有 QMS 正式 schema 的依據；若對接既有 QMS DB，
     表已存在時 OBJECT_ID 檢查會自動跳過，請勿假設與此檔完全一致。
   - 引用來源：
       EMP_USER            ← UserRepository
       DIM_ROLE            ← RoleRepository
       MAP_USER_ROLE       ← RoleRepository / MenuRepository
       SET_FUNCTION        ← MenuRepository
       MAP_RIGHT_FUNCTION  ← MenuRepository
       MAP_ROLE_RIGHT      ← MenuRepository
       SET_PARAM           ← ParameterRepository
       AUTHENTICATION_LOG  ← AuthenticationLogRepository
   ============================================================================= */

/* ---------- EMP_USER：使用者主檔（使用者帳號管理 / 登入） ---------- */
IF OBJECT_ID(N'dbo.EMP_USER', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EMP_USER
    (
        USER_ID     NVARCHAR(50)   NOT NULL,
        USER_NAME   NVARCHAR(100)  NOT NULL,
        PASSWORD    NVARCHAR(200)  NOT NULL,  -- BCrypt hash，不存明碼
        TIT_NAME    NVARCHAR(100)  NULL,
        EMAIL       NVARCHAR(200)  NULL,
        TELEPHONE   NVARCHAR(50)   NULL,
        FACTORY_NO  NVARCHAR(20)   NULL,
        DPT_CODE    NVARCHAR(20)   NULL,
        DEL_FLG     BIT            NOT NULL CONSTRAINT DF_EMP_USER_DEL_FLG DEFAULT (0),
        CRT_DATE    DATETIME2(0)   NULL,
        CRT_USER    NVARCHAR(50)   NULL,
        MDF_DATE    DATETIME2(0)   NULL,
        MDF_USER    NVARCHAR(50)   NULL,
        CONSTRAINT PK_EMP_USER PRIMARY KEY CLUSTERED (USER_ID)
    );
END
GO

/* ---------- DIM_ROLE：角色主檔（角色與權限管理） ---------- */
IF OBJECT_ID(N'dbo.DIM_ROLE', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DIM_ROLE
    (
        ROLE_ID    NVARCHAR(50)   NOT NULL,
        ROLE_NAME  NVARCHAR(100)  NOT NULL,
        ROLE_TYPE  NVARCHAR(20)   NULL,
        DEL_FLG    BIT            NOT NULL CONSTRAINT DF_DIM_ROLE_DEL_FLG DEFAULT (0),
        CRT_DATE   DATETIME2(0)   NULL,
        CRT_USER   NVARCHAR(50)   NULL,
        MDF_DATE   DATETIME2(0)   NULL,
        MDF_USER   NVARCHAR(50)   NULL,
        CONSTRAINT PK_DIM_ROLE PRIMARY KEY CLUSTERED (ROLE_ID)
    );
END
GO

/* ---------- MAP_USER_ROLE：使用者-角色對應 ---------- */
IF OBJECT_ID(N'dbo.MAP_USER_ROLE', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MAP_USER_ROLE
    (
        USER_ID  NVARCHAR(50)  NOT NULL,
        ROLE_ID  NVARCHAR(50)  NOT NULL,
        CRT_DATE DATETIME2(0)  NULL,
        CRT_USER NVARCHAR(50)  NULL,
        CONSTRAINT PK_MAP_USER_ROLE PRIMARY KEY CLUSTERED (USER_ID, ROLE_ID)
    );
END
GO

/* ---------- SET_FUNCTION：系統功能主檔（系統功能管理 / 選單） ---------- */
IF OBJECT_ID(N'dbo.SET_FUNCTION', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SET_FUNCTION
    (
        SEQ_NO        INT            IDENTITY(1,1) NOT NULL,
        FUNCTION_ID   NVARCHAR(50)   NOT NULL,
        FUNCTION_NAME NVARCHAR(100)  NOT NULL,
        FUNCTION_URL  NVARCHAR(500)  NULL,
        PARENT_ID     NVARCHAR(50)   NULL,   -- 樹狀選單，指向上層 FUNCTION_ID
        SORT_ID       SMALLINT       NOT NULL CONSTRAINT DF_SET_FUNCTION_SORT_ID DEFAULT (0),
        DEL_FLG       BIT            NOT NULL CONSTRAINT DF_SET_FUNCTION_DEL_FLG DEFAULT (0),
        CRT_DATE      DATETIME2(0)   NULL,
        CRT_USER      NVARCHAR(50)   NULL,
        MDF_DATE      DATETIME2(0)   NULL,
        MDF_USER      NVARCHAR(50)   NULL,
        CONSTRAINT PK_SET_FUNCTION PRIMARY KEY CLUSTERED (SEQ_NO),
        CONSTRAINT UQ_SET_FUNCTION_FUNCTION_ID UNIQUE (FUNCTION_ID)
    );
END
GO

/* ---------- MAP_RIGHT_FUNCTION：權限-功能對應 ---------- */
IF OBJECT_ID(N'dbo.MAP_RIGHT_FUNCTION', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MAP_RIGHT_FUNCTION
    (
        RIGHT_ID    NVARCHAR(50)  NOT NULL,
        FUNCTION_ID NVARCHAR(50)  NOT NULL,
        CRT_DATE    DATETIME2(0)  NULL,
        CRT_USER    NVARCHAR(50)  NULL,
        CONSTRAINT PK_MAP_RIGHT_FUNCTION PRIMARY KEY CLUSTERED (RIGHT_ID, FUNCTION_ID)
    );
END
GO

/* ---------- MAP_ROLE_RIGHT：角色-權限對應 ---------- */
IF OBJECT_ID(N'dbo.MAP_ROLE_RIGHT', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MAP_ROLE_RIGHT
    (
        ROLE_ID  NVARCHAR(50)  NOT NULL,
        RIGHT_ID NVARCHAR(50)  NOT NULL,
        CRT_DATE DATETIME2(0)  NULL,
        CRT_USER NVARCHAR(50)  NULL,
        CONSTRAINT PK_MAP_ROLE_RIGHT PRIMARY KEY CLUSTERED (ROLE_ID, RIGHT_ID)
    );
END
GO

/* ---------- SET_PARAM：通用系統參數 ---------- */
IF OBJECT_ID(N'dbo.SET_PARAM', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SET_PARAM
    (
        SET_ITEM   NVARCHAR(50)   NOT NULL,
        SET_TYPE   NVARCHAR(50)   NOT NULL,
        SET_VALUE  NVARCHAR(500)  NOT NULL,
        SORT_ORDER INT            NOT NULL CONSTRAINT DF_SET_PARAM_SORT_ORDER DEFAULT (0),
        MEMO       NVARCHAR(500)  NULL,
        DEL_FLG    BIT            NOT NULL CONSTRAINT DF_SET_PARAM_DEL_FLG DEFAULT (0),
        CRT_DATE   DATETIME2(0)   NULL,
        CRT_USER   NVARCHAR(50)   NULL,
        MDF_DATE   DATETIME2(0)   NULL,
        MDF_USER   NVARCHAR(50)   NULL,
        CONSTRAINT PK_SET_PARAM PRIMARY KEY CLUSTERED (SET_ITEM, SET_TYPE)
    );
END
GO

/* ---------- AUTHENTICATION_LOG：登入/登出紀錄（使用者登入軌跡查詢） ---------- */
IF OBJECT_ID(N'dbo.AUTHENTICATION_LOG', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AUTHENTICATION_LOG
    (
        GUID             NVARCHAR(50)   NOT NULL,
        USER_ID          NVARCHAR(50)   NOT NULL,
        IDENTITY_CONTENT NVARCHAR(MAX)  NULL,
        IP               NVARCHAR(50)   NULL,
        LOGIN_TYPE       CHAR(1)        NOT NULL,
        AUTH_STATUS      CHAR(1)        NOT NULL,
        LOGIN_TIME       DATETIME2(0)   NOT NULL,
        LOGOUT_TIME      DATETIME2(0)   NULL,
        CONSTRAINT PK_AUTHENTICATION_LOG PRIMARY KEY CLUSTERED (GUID)
    );

    CREATE NONCLUSTERED INDEX IX_AUTHENTICATION_LOG_USER_TIME
        ON dbo.AUTHENTICATION_LOG (USER_ID, LOGIN_TIME DESC);
END
GO
