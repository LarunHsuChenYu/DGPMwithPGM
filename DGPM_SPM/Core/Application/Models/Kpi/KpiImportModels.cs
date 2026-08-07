namespace DGPM_SPM.Core.Application.Models.Kpi;

/// <summary>
/// 建立 KPI 匯入批次的請求（最小可用版本：由前端表單/文字貼上解析成結構化明細，
/// 不含真實檔案上傳；欄位以暫定 schema kpi.KPI_IMPORT_BATCH / kpi.KPI_DATA 為準）。
/// </summary>
public class CreateKpiImportRequest
{
    /// <summary>匯入資料所屬年月 yyyyMM（批次層級，全部明細共用）。</summary>
    public string PeriodYm { get; set; } = string.Empty;

    /// <summary>來源檔名或來源說明（選填，僅作日誌顯示）。</summary>
    public string? FileName { get; set; }

    public List<KpiImportRowRequest> Rows { get; set; } = [];
}

/// <summary>單筆匯入明細。數值以字串傳入，由伺服器端驗證格式與範圍。</summary>
public class KpiImportRowRequest
{
    public string DealerCode { get; set; } = string.Empty;
    public string IndicatorCode { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>匯入批次 DTO（與「KPI 匯入日誌查詢」共用）。</summary>
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

/// <summary>當次匯入的完整結果：批次彙總 + 逐列結果（逐列結果僅回應本次請求，不落庫）。</summary>
public class KpiImportResultDto
{
    public KpiImportBatchDto Batch { get; set; } = new();
    public List<KpiImportRowResultDto> RowResults { get; set; } = [];
}

/// <summary>單筆明細的處理結果。</summary>
public class KpiImportRowResultDto
{
    /// <summary>明細序號（從 1 起，對應輸入順序）。</summary>
    public int RowNo { get; set; }

    public string DealerCode { get; set; } = string.Empty;
    public string IndicatorCode { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
