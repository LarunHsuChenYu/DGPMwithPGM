namespace DGPM_SPM.Core.Application.Queries;

public class ExchangeRateFilter : FilterBase
{
    public string? CurrencyCode { get; set; }
    public string? RateYmFrom { get; set; }
    public string? RateYmTo { get; set; }
    public string? Status { get; set; }
}
