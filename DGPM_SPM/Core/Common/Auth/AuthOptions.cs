namespace DGPM_SPM.Core.Common.Auth;

/// <summary>DGPM Auth 部署參數（一律外連 PGM；本系統不再支援 Local Auth）。</summary>
public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>為 true 時允許由 DGPM 登入頁進入；false 則拒絕業務系統登入入口。</summary>
    public bool AllowPGMLoginEntry { get; set; } = true;

    public string PgmBaseUrl { get; set; } = "http://localhost:9528";

    /// <summary>PGM Web 根網址（文件／對稱設定用；側欄外連以 Web 專案 Auth:PgmWebBaseUrl 為準）。</summary>
    public string PgmWebBaseUrl { get; set; } = "https://localhost:7230";

    public string SystemCode { get; set; } = "DGPM";
}
