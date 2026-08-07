using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Kpi;

namespace DGPM_SPM.Core.Application.Interfaces;

public interface IKpiDataPermissionService
{
    /// <summary>查詢使用者的 KPI 資料權限（主體資訊 + 目前授權範圍）。</summary>
    Task<ApiResponse<KpiUserPermissionDto>> GetByUserIdAsync(
        string userId,
        CancellationToken ct = default);

    /// <summary>全量覆寫使用者的 KPI 資料權限，回傳儲存後的授權內容。</summary>
    Task<ApiResponse<KpiUserPermissionDto>> SaveAsync(
        string userId,
        SaveKpiUserPermissionRequest request,
        CancellationToken ct = default);
}
