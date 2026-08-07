namespace DGPM_SPM.Core.Domain.Entities;

/// <summary>
/// KPI 匯入批次（kpi.KPI_IMPORT_BATCH，兼作匯入日誌）。
/// ⚠ 對應 SQL/40_kpi_dealer_kpi.sql 之 provisional draft，欄位待 SDS 定稿確認。
/// 本表無 CRT/MDF audit 欄位（以 IMPORT_USER / IMPORT_START / IMPORT_END 留痕），故不繼承 BaseEntity。
/// </summary>
public class KpiImportBatch
{
    public long BatchId { get; set; }

    /// <summary>來源檔名或來源說明（Excel/CSV/手動輸入）。</summary>
    public string? FileName { get; set; }

    /// <summary>匯入資料所屬年月 yyyyMM。</summary>
    public string PeriodYm { get; set; } = string.Empty;

    /// <summary>P=處理中, S=成功, F=失敗（含部分成功）。</summary>
    public string ImportStatus { get; set; } = "P";

    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int FailRows { get; set; }

    /// <summary>失敗摘要；不得含敏感資訊。</summary>
    public string? ErrorMessage { get; set; }

    public string ImportUser { get; set; } = string.Empty;
    public DateTime? ImportStart { get; set; }
    public DateTime? ImportEnd { get; set; }
}
