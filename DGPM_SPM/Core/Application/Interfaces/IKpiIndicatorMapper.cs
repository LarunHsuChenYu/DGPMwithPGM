using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IKpiIndicatorMapper
{
    KpiIndicatorDto ToDto(KpiIndicator kpiIndicator);
    IReadOnlyList<KpiIndicatorDto> ToDtos(IEnumerable<KpiIndicator> kpiIndicators);
}
