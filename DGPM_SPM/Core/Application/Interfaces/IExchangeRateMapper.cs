using DGPM_SPM.Core.Application.Models.ExchangeRate;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IExchangeRateMapper
{
    ExchangeRateDto ToDto(ExchangeRate exchangeRate);
    IReadOnlyList<ExchangeRateDto> ToDtos(IEnumerable<ExchangeRate> exchangeRates);
}
