using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Common.Attributes;
using DGPM_SPM.Core.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace DGPM_SPM.Core.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
[ScopedRegistration]
public partial class KpiIndicatorMapper : IKpiIndicatorMapper
{
    public partial KpiIndicatorDto ToDto(KpiIndicator kpiIndicator);

    public partial IReadOnlyList<KpiIndicatorDto> ToDtos(IEnumerable<KpiIndicator> kpiIndicators);
}
