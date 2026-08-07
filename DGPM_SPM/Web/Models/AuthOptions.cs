namespace DGPM_SPM.Web.Models;

/// <summary>
/// DGPM Auth 部署參數（Web 端唯讀顯示用）。實際登入一律由 DGPM Api
/// （<c>/api/auth/login</c>）轉發至 PGM；本系統不再支援 Local Auth。
/// </summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>是否允許由本系統登入頁進入；false 時提前顯示停用訊息。</summary>
    public bool AllowPGMLoginEntry { get; set; } = true;

    /// <summary>
    /// PGM Web 根網址（側欄 <c>ext:pgm</c>／舊 <c>external:pgm</c> 外連目標）。
    /// Development：<c>https://localhost:7230</c>；Production：<c>http://localhost:8965</c>。
    /// </summary>
    public string PgmWebBaseUrl { get; set; } = string.Empty;
}
