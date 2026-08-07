/* =============================================================================
   30_cfg_system_parameters.sql
   DGPM_SPM — 系統參數管理（cfg schema）

   ⚠ PROVISIONAL DRAFT：SDS 尚未到位，以下為依 sitemap 推導的暫定設計，
     欄位名稱、型別、長度與約束皆可能在 SDS 定稿後調整，不得視為正式規格。

   對應 sitemap：
   - 系統參數管理 / 匯率參數設定 → cfg.EXCHANGE_RATE

   備註：通用鍵值型參數沿用既有 dbo.SET_PARAM（見 10_dbo_qms_compat.sql）；
   匯率因具備「幣別 + 期間 + 精確數值」結構，獨立成表以利查詢與唯一性約束。

   相依：00_create_schemas.sql（cfg schema 必須先存在）
   ============================================================================= */

/* ---------- cfg.EXCHANGE_RATE：匯率參數（按幣別 + 年月） ---------- */
IF OBJECT_ID(N'cfg.EXCHANGE_RATE', N'U') IS NULL
BEGIN
    CREATE TABLE cfg.EXCHANGE_RATE
    (
        RATE_ID       INT            IDENTITY(1,1) NOT NULL,
        CURRENCY_CODE CHAR(3)        NOT NULL,  -- ISO 4217，例如 USD、JPY
        RATE_YM       CHAR(6)        NOT NULL,  -- 適用年月 yyyyMM，例如 202607
        RATE_VALUE    DECIMAL(18,6)  NOT NULL,  -- 對基準幣別之匯率
        STATUS        CHAR(1)        NOT NULL CONSTRAINT DF_EXCHANGE_RATE_STATUS DEFAULT ('A'),  -- A=啟用, I=停用
        MEMO          NVARCHAR(500)  NULL,
        CRT_DATE      DATETIME2(0)   NOT NULL CONSTRAINT DF_EXCHANGE_RATE_CRT_DATE DEFAULT (SYSDATETIME()),
        CRT_USER      NVARCHAR(50)   NOT NULL,
        MDF_DATE      DATETIME2(0)   NULL,
        MDF_USER      NVARCHAR(50)   NULL,
        CONSTRAINT PK_EXCHANGE_RATE PRIMARY KEY CLUSTERED (RATE_ID),
        CONSTRAINT UQ_EXCHANGE_RATE_CCY_YM UNIQUE (CURRENCY_CODE, RATE_YM),
        CONSTRAINT CK_EXCHANGE_RATE_STATUS CHECK (STATUS IN ('A', 'I')),
        CONSTRAINT CK_EXCHANGE_RATE_VALUE CHECK (RATE_VALUE > 0)
    );
END
GO
