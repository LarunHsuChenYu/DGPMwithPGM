/* =============================================================================
   00_create_schemas.sql
   DGPM_SPM — 建立業務 schema（provisional draft，SDS 到位前之暫定設計）

   說明：
   - 既有 QMS 相容表（EMP_USER、DIM_ROLE、SET_PARAM...）位於 dbo，不在此建立 schema。
   - 以下 schema 為 sitemap 業務模組的暫定分區：
       org : 基本資料管理（經銷商、區域組織）
       cfg : 系統參數管理（匯率參數）
       kpi : 經銷商KPI管理（指標、匯入、覆核、異動紀錄、資料權限）
   - 可重複執行（idempotent）。
   ============================================================================= */

IF SCHEMA_ID(N'org') IS NULL
    EXEC(N'CREATE SCHEMA org AUTHORIZATION dbo;');
GO

IF SCHEMA_ID(N'cfg') IS NULL
    EXEC(N'CREATE SCHEMA cfg AUTHORIZATION dbo;');
GO

IF SCHEMA_ID(N'kpi') IS NULL
    EXEC(N'CREATE SCHEMA kpi AUTHORIZATION dbo;');
GO
