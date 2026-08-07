using System.ComponentModel.DataAnnotations;

namespace DGPM_SPM.Web.Models;

public class KpiIndicatorDto
{
    public int IndicatorId { get; set; }
    public string IndicatorCode { get; set; } = string.Empty;
    public string IndicatorName { get; set; } = string.Empty;
    public string? Unit { get; set; }

    /// <summary>N=數值, P=百分比, A=金額</summary>
    public string DataType { get; set; } = string.Empty;

    public byte DecimalPlaces { get; set; }
    public int SortOrder { get; set; }

    /// <summary>A=啟用, I=停用</summary>
    public string Status { get; set; } = string.Empty;

    public string? Memo { get; set; }
    public DateTime? CrtDate { get; set; }
    public string? CrtUser { get; set; }
    public DateTime? MdfDate { get; set; }
    public string? MdfUser { get; set; }
}

public class KpiIndicatorPage
{
    public IReadOnlyList<KpiIndicatorDto> Datas { get; set; } = [];
    public int TotalRow { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
}

public class SaveKpiIndicatorRequest
{
    [Required(ErrorMessage = "請輸入指標代碼")]
    [StringLength(30, ErrorMessage = "指標代碼不可超過 30 字")]
    public string IndicatorCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入指標名稱")]
    [StringLength(200, ErrorMessage = "指標名稱不可超過 200 字")]
    public string IndicatorName { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "單位不可超過 20 字")]
    public string? Unit { get; set; }

    [Required(ErrorMessage = "請選擇資料型態")]
    [RegularExpression("^[NPA]$", ErrorMessage = "資料型態須為 N / P / A")]
    public string DataType { get; set; } = "N";

    [Range(0, 6, ErrorMessage = "小數位數須介於 0 到 6")]
    public byte DecimalPlaces { get; set; } = 2;

    public int SortOrder { get; set; }

    [StringLength(500, ErrorMessage = "備註不可超過 500 字")]
    public string? Memo { get; set; }
}

public class SetKpiIndicatorStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
