using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IKpiChangeLogMapper
{
    KpiChangeLogDto ToDto(KpiChangeLog kpiChangeLog);
    IReadOnlyList<KpiChangeLogDto> ToDtos(IEnumerable<KpiChangeLog> kpiChangeLogs);
}
