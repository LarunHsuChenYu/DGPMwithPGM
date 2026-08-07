using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Dealer;
using DGPM_SPM.Core.Common.Attributes;
using DGPM_SPM.Core.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace DGPM_SPM.Core.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
[ScopedRegistration]
public partial class DealerMapper : IDealerMapper
{
    public partial DealerDto ToDto(Dealer dealer);

    public partial IReadOnlyList<DealerDto> ToDtos(IEnumerable<Dealer> dealers);
}
