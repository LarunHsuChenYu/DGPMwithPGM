namespace DGPM_SPM.Web.Models;

/// <summary>
/// 經銷商儀錶板（Qlik Cloud）嵌入設定。僅含公開資訊（embed URL），
/// 不得放入任何 secret（API key、憑證等）；正式欄位待 Qlik Cloud SDS 定稿後確認。
/// </summary>
public sealed class QlikDashboardOptions
{
    public const string SectionName = "QlikDashboard";

    /// <summary>Qlik Cloud 儀錶板嵌入網址（公開 URL，不含 secret）。未設定時頁面顯示占位說明。</summary>
    public string? EmbedUrl { get; set; }
}
