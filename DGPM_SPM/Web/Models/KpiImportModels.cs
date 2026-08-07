using System.ComponentModel.DataAnnotations;

namespace DGPM_SPM.Web.Models;

/// <summary>建立 KPI 匯入批次的請求（對應 API POST /api/kpi/imports）。</summary>
public class CreateKpiImportRequest
{
    [Required(ErrorMessage = "請輸入資料年月")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "資料年月格式須為 yyyyMM，例如 202607")]
    public string PeriodYm { get; set; } = string.Empty;

    [StringLength(260, ErrorMessage = "來源說明不可超過 260 字")]
    public string? FileName { get; set; }

    public List<KpiImportRowRequest> Rows { get; set; } = [];
}

/// <summary>單筆匯入明細；數值以字串傳遞，由伺服器端驗證。</summary>
public class KpiImportRowRequest
{
    public string DealerCode { get; set; } = string.Empty;
    public string IndicatorCode { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class KpiImportBatchDto
{
    public long BatchId { get; set; }
    public string? FileName { get; set; }
    public string PeriodYm { get; set; } = string.Empty;

    /// <summary>P=處理中, S=成功, F=失敗（含部分成功）。</summary>
    public string ImportStatus { get; set; } = string.Empty;

    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int FailRows { get; set; }
    public string? ErrorMessage { get; set; }
    public string ImportUser { get; set; } = string.Empty;
    public DateTime? ImportStart { get; set; }
    public DateTime? ImportEnd { get; set; }
}

public class KpiImportResultDto
{
    public KpiImportBatchDto Batch { get; set; } = new();
    public List<KpiImportRowResultDto> RowResults { get; set; } = [];
}

public class KpiImportRowResultDto
{
    public int RowNo { get; set; }
    public string DealerCode { get; set; } = string.Empty;
    public string IndicatorCode { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class KpiImportBatchPage
{
    public IReadOnlyList<KpiImportBatchDto> Datas { get; set; } = [];
    public int TotalRow { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
}
