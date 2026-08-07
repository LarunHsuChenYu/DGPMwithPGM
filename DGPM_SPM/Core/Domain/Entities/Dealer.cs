namespace DGPM_SPM.Core.Domain.Entities;

/// <summary>
/// 經銷商主檔（org.DEALER）。
/// ⚠ 對應 SQL/20_org_master_data.sql 之 provisional draft，欄位待 SDS 定稿確認。
/// </summary>
public class Dealer : BaseEntity
{
    public int DealerId { get; set; }
    public string DealerCode { get; set; } = string.Empty;
    public string DealerName { get; set; } = string.Empty;
    public int RegionId { get; set; }

    /// <summary>交易幣別（ISO 4217，3 碼）。</summary>
    public string? CurrencyCode { get; set; }

    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactTel { get; set; }

    /// <summary>A=啟用, I=停用。</summary>
    public string Status { get; set; } = "A";

    public string? Memo { get; set; }

    /// <summary>查詢時 JOIN org.REGION 附帶的區域名稱；非 DEALER 資料表欄位。</summary>
    public string? RegionName { get; set; }
}
