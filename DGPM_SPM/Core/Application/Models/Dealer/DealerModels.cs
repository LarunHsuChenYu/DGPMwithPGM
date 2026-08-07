namespace DGPM_SPM.Core.Application.Models.Dealer;

/// <summary>經銷商列表/明細 DTO。⚠ 欄位對應 org.DEALER provisional draft，待 SDS 定稿確認。</summary>
public class DealerDto
{
    public int DealerId { get; set; }
    public string DealerCode { get; set; } = string.Empty;
    public string DealerName { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public string? RegionName { get; set; }
    public string? CurrencyCode { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactTel { get; set; }

    /// <summary>A=啟用, I=停用。</summary>
    public string Status { get; set; } = string.Empty;

    public string? Memo { get; set; }
}

/// <summary>
/// 新增/編輯經銷商的請求。DealerId 由路由（編輯）決定；Status 由啟停用 API 維護。
/// </summary>
public class DealerSaveRequest
{
    public string DealerCode { get; set; } = string.Empty;
    public string DealerName { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public string? CurrencyCode { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactTel { get; set; }
    public string? Memo { get; set; }
}

/// <summary>啟用/停用經銷商的請求。</summary>
public class DealerStatusRequest
{
    /// <summary>A=啟用, I=停用。</summary>
    public string Status { get; set; } = string.Empty;
}
