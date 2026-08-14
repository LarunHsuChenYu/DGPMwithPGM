using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;

namespace PGM.Core.Application.Interfaces;

public interface IPgmUiModeService
{
    Task<string> GetModeValueAsync(CancellationToken ct = default);

    Task<ApiResponse<PgmUiModeDto>> GetAsync(CancellationToken ct = default);

    /// <summary>僅 PGMAdmin（或舊 ADMIN 角色）可寫；寫入 SET_PARAM（Auth／PgmUiMode）。</summary>
    Task<ApiResponse<PgmUiModeDto>> SetAsync(UpdatePgmUiModeRequest request, CancellationToken ct = default);
}
