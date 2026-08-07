using System.ComponentModel.DataAnnotations;

namespace DGPM_SPM.Web.Models;

/// <summary>對應後端 DealerDto transport contract；欄位異動時需與 Api 同步。</summary>
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
    public string Status { get; set; } = string.Empty;
    public string? Memo { get; set; }
}

public class DealerSaveRequest
{
    [Required(ErrorMessage = "請輸入經銷商代碼")]
    [StringLength(20, ErrorMessage = "經銷商代碼不可超過 20 字元")]
    public string DealerCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入經銷商名稱")]
    [StringLength(200, ErrorMessage = "經銷商名稱不可超過 200 字元")]
    public string DealerName { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "請選擇所屬區域")]
    public int RegionId { get; set; }

    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "幣別須為 3 碼英文字母，例如 TWD")]
    public string? CurrencyCode { get; set; }

    [StringLength(100, ErrorMessage = "聯絡人姓名不可超過 100 字元")]
    public string? ContactName { get; set; }

    [EmailAddress(ErrorMessage = "Email 格式不正確")]
    [StringLength(200, ErrorMessage = "Email 不可超過 200 字元")]
    public string? ContactEmail { get; set; }

    [StringLength(50, ErrorMessage = "聯絡電話不可超過 50 字元")]
    public string? ContactTel { get; set; }

    [StringLength(500, ErrorMessage = "備註不可超過 500 字元")]
    public string? Memo { get; set; }
}

public class DealerStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
