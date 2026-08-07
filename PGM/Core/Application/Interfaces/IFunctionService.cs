using PGM.Core.Application.Models;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Functions;
using PGM.Core.Application.Queries;

namespace PGM.Core.Application.Interfaces;

public interface IFunctionService
{
    Task<ApiResponse<PagedResult<FunctionDto>>> GetPagedAsync(
        FunctionFilter filter,
        CancellationToken ct = default);

    Task<ApiResponse<FunctionDto?>> GetByFunIdAsync(string funId, CancellationToken ct = default);

    /// <summary>上層選單下拉（Del_YN=N 且 Action_Type=M）。</summary>
    Task<ApiResponse<List<FunctionOptionDto>>> GetParentOptionsAsync(CancellationToken ct = default);

    /// <summary>編輯表單父節點下拉；excludeFunId 排除自身與子孫。</summary>
    Task<ApiResponse<List<FunctionOptionDto>>> GetOptionsAsync(
        string? excludeFunId,
        CancellationToken ct = default);

    Task<ApiResponse<FunctionDto?>> CreateAsync(
        SaveFunctionRequest request,
        CancellationToken ct = default);

    Task<ApiResponse<FunctionDto?>> UpdateAsync(
        string funId,
        SaveFunctionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// 刪除前檢核：不可有未刪除子層，且不可被角色權限引用（MAP_RIGHT_FUNCTION／SysRoleFun）。
    /// </summary>
    Task<ApiResponse<bool>> CanDeleteAsync(string funId, CancellationToken ct = default);

    /// <summary>軟刪（Del_YN=Y）；需通過 <see cref="CanDeleteAsync"/> 檢核。</summary>
    Task<ApiResponse<bool>> SoftDeleteAsync(string funId, CancellationToken ct = default);
}
