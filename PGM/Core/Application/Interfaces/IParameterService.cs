using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Parameter;

namespace PGM.Core.Application.Interfaces;

public interface IParameterService
{
    /// <summary>相容讀取：參數清單（6 小時 MemoryCache）。</summary>
    Task<ApiResponse<List<ParameterItemDto>>> GetParameterListAsync(string setItem, CancellationToken ct = default);

    Task<ApiResponse<IReadOnlyList<ParameterCategoryDto>>> GetCategoriesAsync(CancellationToken ct = default);

    Task<ApiResponse<IReadOnlyList<ParameterDto>>> GetByCategoryAsync(string setItem, CancellationToken ct = default);

    Task<ApiResponse<int>> GetNextSortOrderAsync(string setItem, CancellationToken ct = default);

    Task<ApiResponse<ParameterDto?>> CreateAsync(CreateParameterRequest request, CancellationToken ct = default);

    Task<ApiResponse<ParameterDto?>> UpdateAsync(
        string setItem,
        string setId,
        UpdateParameterRequest request,
        CancellationToken ct = default);

    Task<ApiResponse<bool>> DeleteAsync(string setItem, string setId, CancellationToken ct = default);
}
