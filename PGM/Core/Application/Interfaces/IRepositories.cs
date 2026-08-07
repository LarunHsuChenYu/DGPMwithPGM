using PGM.Core.Domain.Entities;
using PGM.Core.Application.Models;
using PGM.Core.Application.Queries;

namespace PGM.Core.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<PagedResult<User>> GetPagedAsync(UserAccountFilter filter, CancellationToken ct = default);
    Task<User?> GetForManagementAsync(string userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(string userId, CancellationToken ct = default);
    Task<int> AddAsync(User entity, CancellationToken ct = default);
    Task<int> UpdateAsync(User entity, CancellationToken ct = default);
    Task<int> UpdateStatusAsync(User entity, CancellationToken ct = default);
    /// <summary>更新密碼 hash，並寫入 CHANGE_PASSWORD_HISTORY。</summary>
    Task UpdatePasswordAsync(
        string userId,
        string passwordHash,
        string auditUser,
        DateTime auditDate,
        CancellationToken ct = default);
    Task ReplaceRolesAsync(
        string userId,
        IReadOnlyCollection<string> roleIds,
        string auditUser,
        DateTime auditDate,
        CancellationToken ct = default);
}

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> GetAllByUserIdAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetAllActiveAsync(CancellationToken ct = default);
    Task<PagedResult<Role>> GetPagedAsync(RoleFilter filter, CancellationToken ct = default);

    /// <summary>依角色代碼取得角色（含已停用，供管理功能使用）。</summary>
    Task<Role?> GetByIdAsync(string roleId, CancellationToken ct = default);

    Task<bool> ExistsAsync(string roleId, CancellationToken ct = default);
    Task<int> AddAsync(Role entity, CancellationToken ct = default);
    Task<int> UpdateAsync(Role entity, CancellationToken ct = default);
    Task<int> UpdateStatusAsync(Role entity, CancellationToken ct = default);

    /// <summary>該角色目前有效授權的功能（經 MAP_ROLE_RIGHT → MAP_RIGHT_FUNCTION 展開後去重）。</summary>
    Task<IReadOnlyList<string>> GetGrantedFunctionIdsAsync(string roleId, CancellationToken ct = default);

    /// <summary>
    /// 功能是否已被任一角色授權引用（MAP_RIGHT_FUNCTION／角色×功能關聯）。
    /// </summary>
    Task<bool> IsFunctionReferencedAsync(string funId, CancellationToken ct = default);

    /// <summary>
    /// 以勾選的功能全量取代該角色授權：改為單一專屬 RIGHT（RIGHT_ID = ROLE_ID）
    /// 並重建其功能對應；不影響其他角色與共用 RIGHT 的既有資料。
    /// </summary>
    Task ReplaceFunctionsAsync(
        string roleId,
        IReadOnlyCollection<string> functionIds,
        string auditUser,
        DateTime auditDate,
        CancellationToken ct = default);
}

public interface IMenuRepository
{
    /// <summary>
    /// 側邊選單：依使用者任一有效角色（UNION）→ MAP_ROLE_FUNCTION → SET_FUNCTION。
    /// 角色切換請改用 <see cref="GetMenuByRoleIdAsync"/>。
    /// </summary>
    Task<IReadOnlyList<SysFun>> GetMenuByUserIdAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// 側邊選單／權限：依單一 ROLE_ID → MAP_ROLE_FUNCTION → SET_FUNCTION
    /// （IS_MENU=Y、IS_ENABLED=Y、DEL_FLG=0）。授權葉功能時自動帶出父模組 M。
    /// </summary>
    /// <param name="systemCode">非空時依 SET_FUNCTION.SYSTEM_CODE 過濾。</param>
    Task<IReadOnlyList<SysFun>> GetMenuByRoleIdAsync(
        string roleId,
        string? systemCode = null,
        CancellationToken ct = default);

    /// <summary>未刪除且啟用中的系統功能，供角色授權勾選清單使用。</summary>
    Task<IReadOnlyList<SysFun>> GetAllActiveAsync(CancellationToken ct = default);
}

public interface IFunctionRepository
{
    Task<PagedResult<SysFun>> GetPagedAsync(FunctionFilter filter, CancellationToken ct = default);
    Task<SysFun?> GetByFunIdAsync(string funId, CancellationToken ct = default);

    /// <summary>查詢用上層選單：Del_YN=N 且 Action_Type=M。</summary>
    Task<IReadOnlyList<SysFun>> GetModuleOptionsAsync(CancellationToken ct = default);

    /// <summary>編輯表單父節點下拉；excludeFunId 排除自身與其子孫。</summary>
    Task<IReadOnlyList<SysFun>> GetActiveOptionsAsync(string? excludeFunId, CancellationToken ct = default);

    Task<bool> ExistsFunIdAsync(string funId, CancellationToken ct = default);

    /// <summary>candidateFunId 是否為 funId 的子孫節點（僅未刪除）。</summary>
    Task<bool> IsDescendantAsync(string funId, string candidateFunId, CancellationToken ct = default);

    /// <summary>是否仍有未刪除（Del_YN=N）的子層。</summary>
    Task<bool> HasActiveChildrenAsync(string funId, CancellationToken ct = default);

    Task<int> AddAsync(SysFun entity, CancellationToken ct = default);
    Task<int> UpdateAsync(SysFun entity, CancellationToken ct = default);
    Task<int> SoftDeleteAsync(SysFun entity, CancellationToken ct = default);
}

public interface IParameterRepository
{
    /// <summary>活動中參數細項（相容讀取／快取）。</summary>
    Task<IReadOnlyList<Parameter>> GetAllByItemAsync(string setItem, CancellationToken ct = default);

    /// <summary>活動中代碼類別（SET_PARAMITEM，ParamSet 下拉）。</summary>
    Task<IReadOnlyList<ParamItem>> GetActiveCategoriesAsync(CancellationToken ct = default);

    /// <summary>依類別查 Grid（雙方 DEL_FLG=0，依 SORT_ORDER）。</summary>
    Task<IReadOnlyList<Parameter>> GetActiveByItemJoinAsync(string setItem, CancellationToken ct = default);

    /// <summary>依複合鍵讀取（含已刪，供復活判斷）。</summary>
    Task<Parameter?> GetByKeyAsync(string setItem, string setId, CancellationToken ct = default);

    Task<bool> IsCategoryActiveAsync(string setItem, CancellationToken ct = default);

    Task<string?> GetCategoryNameAsync(string setItem, CancellationToken ct = default);

    Task<int> GetNextSortOrderAsync(string setItem, CancellationToken ct = default);

    Task<int> AddAsync(Parameter entity, CancellationToken ct = default);

    /// <summary>更新代碼名稱／排序（僅活動列）。</summary>
    Task<int> UpdateAsync(Parameter entity, CancellationToken ct = default);

    /// <summary>復活已刪列並更新值／排序。</summary>
    Task<int> ReviveAsync(Parameter entity, CancellationToken ct = default);

    Task<int> SoftDeleteAsync(Parameter entity, CancellationToken ct = default);
}

public interface IAuthenticationLogRepository
{
    Task<int> AddAsync(AuthenticationLog entity, CancellationToken ct = default);
    Task<int> UpdateLogoutAsync(AuthenticationLog entity, CancellationToken ct = default);

    /// <summary>分頁查詢登入/登出軌跡（使用者登入軌跡查詢）。</summary>
    Task<PagedResult<AuthenticationLog>> GetPagedAsync(
        AuthenticationLogFilter filter,
        CancellationToken ct = default);
}
