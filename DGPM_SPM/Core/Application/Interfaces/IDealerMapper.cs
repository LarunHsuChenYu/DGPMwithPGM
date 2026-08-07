using DGPM_SPM.Core.Application.Models.Dealer;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IDealerMapper
{
    DealerDto ToDto(Dealer dealer);
    IReadOnlyList<DealerDto> ToDtos(IEnumerable<Dealer> dealers);
}
