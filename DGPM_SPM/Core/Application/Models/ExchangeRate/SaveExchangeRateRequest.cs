namespace DGPM_SPM.Core.Application.Models.ExchangeRate;

public class SaveExchangeRateRequest
{
    public string CurrencyCode { get; set; } = string.Empty;
    public string RateYm { get; set; } = string.Empty;
    public decimal RateValue { get; set; }
    public string? Memo { get; set; }
}

public class SetExchangeRateStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
