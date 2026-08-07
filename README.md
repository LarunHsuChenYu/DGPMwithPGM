# DGPMwithPGM

作品集專案：**PGM（權限／系統管理平台）** + **DGPM_SPM（專案／經銷／KPI 業務系統）**。

兩者皆以 .NET 10 + Clean Architecture 實作；帳號、角色、功能授權由 **PGM** 主責，DGPM 透過 HTTP 轉發／共用 JWT 接入。

## 目錄結構

```text
DGPMwithPGM/
├── PGM/          # 權限平台（Auth / Role / Function / Parameter）
└── DGPM_SPM/     # 業務系統（經銷商、KPI…；Auth 轉發 PGM）
```

| 專案 | 說明 | 進入點 |
|------|------|--------|
| **PGM** | 獨立權限平台：JWT、角色、功能選單、登入歷程 | [`PGM/README.md`](PGM/README.md) |
| **DGPM_SPM** | DGPM 業務應用；登入／權限委派 PGM | [`DGPM_SPM/README.md`](DGPM_SPM/README.md) |

## 架構關係

```mermaid
flowchart LR
  Browser["瀏覽器"]
  DgpmWeb["DGPM Web"]
  DgpmApi["DGPM Api"]
  PgmWeb["PGM Web"]
  PgmApi["PGM Api"]
  Db[("SQL Server")]

  Browser --> DgpmWeb
  Browser --> PgmWeb
  DgpmWeb --> DgpmApi
  PgmWeb --> PgmApi
  DgpmApi -->|"Auth 轉發 / JWT 驗證"| PgmApi
  DgpmApi --> Db
  PgmApi --> Db
```

## 技術棧

- **.NET 10**／ASP.NET Core／Blazor Server
- **Clean Architecture**：`Api` → `Core` ← `Infrastructure`（Core 零對外 ProjectReference）
- **Dapper**、**Mapperly**、**NLog**、**xUnit**、**NetArchTest**

## 本機快速開始（摘要）

詳見各子專案 README／安裝文件。典型流程：

1. 準備 SQL Server，套用 `PGM/SQL`、`DGPM_SPM/SQL` 腳本  
2. 設定 User Secrets（JWT SecretKey ≥ 32、ConnectionString）  
3. 先啟動 **PGM Api／Web**，再啟動 **DGPM Api／Web**  
4. 以 PGM 建立帳號／角色／功能後，從 DGPM 登入（系統代碼 `DGPM`）

## 安全說明（作品集版）

本倉庫已移除真實密碼、JWT Secret 與內網連線字串；`web.config`／Production 設定改為占位符。請以環境變數或 User Secrets 填入自己的值，**勿**把真實密鑰提交回公開 repo。

## License

MIT（若 GitHub 倉庫已附加授權檔，以其為準）
