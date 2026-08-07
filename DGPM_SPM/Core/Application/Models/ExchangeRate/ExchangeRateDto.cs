namespace DGPM_SPM.Core.Application.Models.ExchangeRate;

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
