/* =============================================================================
   20_org_master_data.sql
   DGPM_SPM — 基本資料管理（org schema）

   ⚠ PROVISIONAL DRAFT：SDS 尚未到位，以下為依 sitemap 推導的暫定設計，
     欄位名稱、型別、長度與約束皆可能在 SDS 定稿後調整，不得視為正式規格。

   對應 sitemap：
   - 基本資料管理 / 區域組織管理  → org.REGION
   - 基本資料管理 / 經銷商設定管理 → org.DEALER

   相依：00_create_schemas.sql（org schema 必須先存在）
   ============================================================================= */

/* ---------- org.REGION：區域組織（支援樹狀階層） ---------- */
IF OBJECT_ID(N'org.REGION', N'U') IS NULL
BEGIN
    CREATE TABLE org.REGION
    (
        REGION_ID        INT            IDENTITY(1,1) NOT NULL,
        REGION_CODE      NVARCHAR(20)   NOT NULL,  -- 業務代碼，穩定識別
        REGION_NAME      NVARCHAR(100)  NOT NULL,
        PARENT_REGION_ID INT            NULL,      -- 上層區域；NULL = 最上層
        SORT_ORDER       INT            NOT NULL CONSTRAINT DF_REGION_SORT_ORDER DEFAULT (0),
        STATUS           CHAR(1)        NOT NULL CONSTRAINT DF_REGION_STATUS DEFAULT ('A'),  -- A=啟用, I=停用
        CRT_DATE         DATETIME2(0)   NOT NULL CONSTRAINT DF_REGION_CRT_DATE DEFAULT (SYSDATETIME()),
        CRT_USER         NVARCHAR(50)   NOT NULL,
        MDF_DATE         DATETIME2(0)   NULL,
        MDF_USER         NVARCHAR(50)   NULL,
        CONSTRAINT PK_REGION PRIMARY KEY CLUSTERED (REGION_ID),
        CONSTRAINT UQ_REGION_CODE UNIQUE (REGION_CODE),
        CONSTRAINT FK_REGION_PARENT FOREIGN KEY (PARENT_REGION_ID) REFERENCES org.REGION (REGION_ID),
        CONSTRAINT CK_REGION_STATUS CHECK (STATUS IN ('A', 'I'))
    );
END
GO

/* ---------- org.DEALER：經銷商主檔 ---------- */
IF OBJECT_ID(N'org.DEALER', N'U') IS NULL
BEGIN
    CREATE TABLE org.DEALER
    (
        DEALER_ID     INT            IDENTITY(1,1) NOT NULL,
        DEALER_CODE   NVARCHAR(20)   NOT NULL,  -- 業務代碼，穩定識別
        DEALER_NAME   NVARCHAR(200)  NOT NULL,
        REGION_ID     INT            NOT NULL,  -- 所屬區域
        CURRENCY_CODE CHAR(3)        NULL,      -- 交易幣別（ISO 4217），配合匯率參數
        CONTACT_NAME  NVARCHAR(100)  NULL,
        CONTACT_EMAIL NVARCHAR(200)  NULL,
        CONTACT_TEL   NVARCHAR(50)   NULL,
        STATUS        CHAR(1)        NOT NULL CONSTRAINT DF_DEALER_STATUS DEFAULT ('A'),  -- A=啟用, I=停用
        MEMO          NVARCHAR(500)  NULL,
        CRT_DATE      DATETIME2(0)   NOT NULL CONSTRAINT DF_DEALER_CRT_DATE DEFAULT (SYSDATETIME()),
        CRT_USER      NVARCHAR(50)   NOT NULL,
        MDF_DATE      DATETIME2(0)   NULL,
        MDF_USER      NVARCHAR(50)   NULL,
        CONSTRAINT PK_DEALER PRIMARY KEY CLUSTERED (DEALER_ID),
        CONSTRAINT UQ_DEALER_CODE UNIQUE (DEALER_CODE),
        CONSTRAINT FK_DEALER_REGION FOREIGN KEY (REGION_ID) REFERENCES org.REGION (REGION_ID),
        CONSTRAINT CK_DEALER_STATUS CHECK (STATUS IN ('A', 'I'))
    );

    CREATE NONCLUSTERED INDEX IX_DEALER_REGION ON org.DEALER (REGION_ID);
END
GO
