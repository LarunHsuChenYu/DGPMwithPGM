using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Common.Attributes;
using DGPM_SPM.Core.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace DGPM_SPM.Core.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
[ScopedRegistration]
public partial class KpiImportMapper : IKpiImportMapper
{
    public partial KpiImportBatchDto ToDto(KpiImportBatch batch);

    public partial IReadOnlyList<KpiImportBatchDto> ToDtos(IEnumerable<KpiImportBatch> batches);
}
