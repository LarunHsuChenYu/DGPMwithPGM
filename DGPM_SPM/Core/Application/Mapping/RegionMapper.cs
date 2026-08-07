using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Models.Region;
using DGPM_SPM.Core.Common.Attributes;
using DGPM_SPM.Core.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace DGPM_SPM.Core.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
[ScopedRegistration]
public partial class RegionMapper : IRegionMapper
{
    public partial RegionDto ToDto(Region region);

    public partial IReadOnlyList<RegionDto> ToDtos(IEnumerable<Region> regions);

    public partial RegionOptionDto ToOptionDto(Region region);

    public partial IReadOnlyList<RegionOptionDto> ToOptionDtos(IEnumerable<Region> regions);
}
