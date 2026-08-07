using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IKpiImportMapper
{
    KpiImportBatchDto ToDto(KpiImportBatch batch);
    IReadOnlyList<KpiImportBatchDto> ToDtos(IEnumerable<KpiImportBatch> batches);
}
