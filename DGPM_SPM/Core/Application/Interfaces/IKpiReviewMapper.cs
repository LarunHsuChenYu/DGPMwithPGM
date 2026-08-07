using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IKpiReviewMapper
{
    KpiDataDto ToDto(KpiData kpiData);
    IReadOnlyList<KpiDataDto> ToDtos(IEnumerable<KpiData> kpiDatas);
}
