using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Parameter;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IParameterService
{
    Task<ApiResponse<List<ParameterItemDto>>> GetParameterListAsync(string setItem, CancellationToken ct = default);
}
