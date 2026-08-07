namespace DGPM_SPM.Core.Application.Queries;

/// <summary>KPI 異動紀錄查詢條件（分頁由 FilterBase 提供）。</summary>
public class KpiChangeLogFilter : FilterBase
{
    /// <summary>資料年月 yyyyMM；null 表示不篩選。</summary>
    public string? PeriodYm { get; set; }

    /// <summary>關鍵字，模糊比對經銷商或指標的代碼、名稱。</summary>
    public string? Keyword { get; set; }

    /// <summary>異動類型（I=匯入, M=修改, R=覆核, U=解鎖）；null 表示不篩選。</summary>
    public string? ActionType { get; set; }

    /// <summary>異動日期起（含當日）；null 表示不篩選。</summary>
    public DateTime? ActionDateFrom { get; set; }

    /// <summary>異動日期迄（含當日）；null 表示不篩選。</summary>
    public DateTime? ActionDateTo { get; set; }

    /// <summary>操作者帳號，模糊比對；null 表示不篩選。</summary>
    public string? ActionUser { get; set; }
}
