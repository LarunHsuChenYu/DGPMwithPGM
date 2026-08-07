using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.ExchangeRate;
using DGPM_SPM.Core.Common.Attributes;
using DGPM_SPM.Core.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace DGPM_SPM.Core.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
[ScopedRegistration]
public partial class ExchangeRateMapper : IExchangeRateMapper
{
    public partial ExchangeRateDto ToDto(ExchangeRate exchangeRate);

    public partial IReadOnlyList<ExchangeRateDto> ToDtos(IEnumerable<ExchangeRate> exchangeRates);
}
