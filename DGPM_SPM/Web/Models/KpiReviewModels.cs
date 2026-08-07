using System.ComponentModel.DataAnnotations;

namespace DGPM_SPM.Web.Models;

/// <summary>對應後端 KpiDataDto（KPI 數據覆核列表）。</summary>
public class KpiDataDto
{
    public long DataId { get; set; }
    public int DealerId { get; set; }
    public string? DealerCode { get; set; }
    public string? DealerName { get; set; }
    public int IndicatorId { get; set; }
    public string? IndicatorCode { get; set; }
    public string? IndicatorName { get; set; }
    public string? Unit { get; set; }

    /// <summary>資料年月 yyyyMM</summary>
    public string PeriodYm { get; set; } = string.Empty;

    public decimal? KpiValue { get; set; }
    public long? BatchId { get; set; }

    /// <summary>D=草稿, R=覆核完成(鎖定), U=已解鎖待修正</summary>
    public string ReviewStatus { get; set; } = string.Empty;

    public string? ReviewUser { get; set; }
    public DateTime? ReviewDate { get; set; }
    public DateTime? CrtDate { get; set; }
    public DateTime? MdfDate { get; set; }
}

public class KpiDataPage
{
    public IReadOnlyList<KpiDataDto> Datas { get; set; } = [];
    public int TotalRow { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
}

/// <summary>覆核確認請求（備註選填）。</summary>
public class ReviewKpiDataRequest
{
    [StringLength(500, ErrorMessage = "覆核備註不可超過 500 字")]
    public string? Memo { get; set; }
}

/// <summary>解鎖退回請求（原因必填）。</summary>
public class UnlockKpiDataRequest
{
    [Required(ErrorMessage = "請輸入解鎖原因")]
    [StringLength(500, ErrorMessage = "解鎖原因不可超過 500 字")]
    public string Reason { get; set; } = string.Empty;
}
