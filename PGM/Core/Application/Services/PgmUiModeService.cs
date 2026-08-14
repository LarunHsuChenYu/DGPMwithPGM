using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.Auth;
using PGM.Core.Application.Models.Enums;
using PGM.Core.Common.Attributes;
using PGM.Core.Common.Auth;
using PGM.Core.Common.Extensions;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Application.Services;

/// <summary>讀寫 SET_PARAM（Auth／PgmUiMode）；寫入限 PGMAdmin（或舊 ADMIN 角色）。</summary>
[ScopedRegistration]
public class PgmUiModeService : IPgmUiModeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IRequestContext _requestContext;

    public PgmUiModeService(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IRequestContext requestContext)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _requestContext = requestContext;
    }

    public async Task<string> GetModeValueAsync(CancellationToken ct = default)
    {
        var row = await _unitOfWork.Parameters.GetByKeyAsync(
            PgmUiMode.SetItem, PgmUiMode.SetId, ct);
        return PgmUiMode.Normalize(row?.SetValue);
    }

    public async Task<ApiResponse<PgmUiModeDto>> GetAsync(CancellationToken ct = default)
    {
        var mode = await GetModeValueAsync(ct);
        return ApiResponse<PgmUiModeDto>.SuccessResult(
            new PgmUiModeDto
            {
                Mode = mode,
                CanEdit = await IsPgmAdminUserAsync(ct)
            },
            traceId: _requestContext.TraceId);
    }

    public async Task<ApiResponse<PgmUiModeDto>> SetAsync(
        UpdatePgmUiModeRequest request,
        CancellationToken ct = default)
    {
        var traceId = _requestContext.TraceId;
        if (!await IsPgmAdminUserAsync(ct))
        {
            return ApiResponse<PgmUiModeDto>.ErrorResult(
                ErrorCodes.UnauthorizedAccess.GetDescription("code"),
                "僅 PGMAdmin 可切換系統權限 UI Mode。",
                traceId);
        }

        if (!PgmUiMode.IsOn(request.Mode) && !PgmUiMode.IsOff(request.Mode))
        {
            return ApiResponse<PgmUiModeDto>.ErrorResult(
                ErrorCodes.InvalidParameter.GetDescription("code"),
                "Mode 僅允許 On 或 Off。",
                traceId);
        }

        var mode = PgmUiMode.Normalize(request.Mode);
        var auditUser = string.IsNullOrWhiteSpace(_currentUser.UserId) ? "SYSTEM" : _currentUser.UserId!;
        var now = DateTime.Now;

        if (!await _unitOfWork.Parameters.IsCategoryActiveAsync(PgmUiMode.SetItem, ct))
        {
            return ApiResponse<PgmUiModeDto>.ErrorResult(
                ErrorCodes.DataNotFound.GetDescription("code"),
                "缺少 SET_PARAMITEM（Auth）。請先執行種子腳本。",
                traceId);
        }

        var existing = await _unitOfWork.Parameters.GetByKeyAsync(
            PgmUiMode.SetItem, PgmUiMode.SetId, ct);

        await _unitOfWork.BeginTransactionAsync(ct: ct);
        try
        {
            if (existing is null)
            {
                await _unitOfWork.Parameters.AddAsync(new Parameter
                {
                    SetItem = PgmUiMode.SetItem,
                    SetId = PgmUiMode.SetId,
                    SetValue = mode,
                    SortOrder = 1,
                    Memo = "On＝系統權限 UI 在 PGM；Off＝在 DGPM",
                    DelFlg = false,
                    CrtDate = now,
                    CrtUser = auditUser
                }, ct);
            }
            else if (existing.DelFlg)
            {
                existing.SetValue = mode;
                existing.SortOrder = 1;
                existing.MdfDate = now;
                existing.MdfUser = auditUser;
                await _unitOfWork.Parameters.ReviveAsync(existing, ct);
            }
            else
            {
                existing.SetValue = mode;
                existing.MdfDate = now;
                existing.MdfUser = auditUser;
                await _unitOfWork.Parameters.UpdateAsync(existing, ct);
            }

            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }

        return ApiResponse<PgmUiModeDto>.SuccessResult(
            new PgmUiModeDto { Mode = mode, CanEdit = true },
            traceId: traceId);
    }

    private async Task<bool> IsPgmAdminUserAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.UserId))
            return false;

        // JWT 可能是 DGPMAdmin（在 DGPM 操作）；仍以帳號是否掛 PGMAdmin／舊 ADMIN 為準
        if (PgmUiMode.IsModeToggleRole(_currentUser.RoleId))
            return true;

        var roles = await _unitOfWork.Roles.GetAllByUserIdAsync(_currentUser.UserId, ct);
        return roles.Any(r => PgmUiMode.IsModeToggleRole(r.RoleId) && r.DelFlg != true);
    }
}
