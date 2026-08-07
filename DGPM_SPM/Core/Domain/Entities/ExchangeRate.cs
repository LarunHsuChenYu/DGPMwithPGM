namespace DGPM_SPM.Core.Domain.Entities;

public class ExchangeRate : BaseEntity
{
    public int RateId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string RateYm { get; set; } = string.Empty;
    public decimal RateValue { get; set; }
    public string Status { get; set; } = "A";
    public string? Memo { get; set; }
}
