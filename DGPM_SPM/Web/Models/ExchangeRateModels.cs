using System.ComponentModel.DataAnnotations;

namespace DGPM_SPM.Web.Models;

public class ExchangeRateDto
{
    public int RateId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string RateYm { get; set; } = string.Empty;
    public decimal RateValue { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Memo { get; set; }
    public DateTime? CrtDate { get; set; }
    public string? CrtUser { get; set; }
    public DateTime? MdfDate { get; set; }
    public string? MdfUser { get; set; }
}

public class ExchangeRatePage
{
    public IReadOnlyList<ExchangeRateDto> Datas { get; set; } = [];
    public int TotalRow { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
}

public class SaveExchangeRateRequest
{
    [Required(ErrorMessage = "請輸入幣別")]
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "幣別須為 3 碼英文字母")]
    public string CurrencyCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入生效年月")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "生效年月格式須為 yyyyMM")]
    public string RateYm { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.000001", "999999999999.999999", ErrorMessage = "匯率必須大於 0")]
    public decimal RateValue { get; set; }

    [StringLength(500, ErrorMessage = "備註不可超過 500 字")]
    public string? Memo { get; set; }
}

public class SetExchangeRateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
