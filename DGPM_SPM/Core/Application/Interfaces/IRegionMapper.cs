using DGPM_SPM.Core.Application.Models.Region;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IRegionMapper
{
    RegionDto ToDto(Region region);
    IReadOnlyList<RegionDto> ToDtos(IEnumerable<Region> regions);
    RegionOptionDto ToOptionDto(Region region);
    IReadOnlyList<RegionOptionDto> ToOptionDtos(IEnumerable<Region> regions);
}
