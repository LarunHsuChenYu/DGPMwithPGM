using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IKpiUserDataScopeMapper
{
    KpiUserDataScopeDto ToDto(KpiUserDataScope scope);
    IReadOnlyList<KpiUserDataScopeDto> ToDtos(IEnumerable<KpiUserDataScope> scopes);
}
