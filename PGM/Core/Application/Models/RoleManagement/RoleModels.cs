namespace PGM.Core.Application.Models.RoleManagement;

/// <summary>角色列表與明細 DTO。沿用 dbo.DIM_ROLE 相容結構（SDS 前暫定）。</summary>
public class RoleDto
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? RoleType { get; set; }
    /// <summary>系統隔離：PGM／DGPM。</summary>
    public string SystemCode { get; set; } = "PGM";

    /// <summary>true=已停用（DEL_FLG=1）。</summary>
    public bool DelFlg { get; set; }

    public DateTime? CrtDate { get; set; }
    public string? CrtUser { get; set; }
    public DateTime? MdfDate { get; set; }
    public string? MdfUser { get; set; }
}

/// <summary>新增角色請求。RoleId 為使用者自訂代碼（DIM_ROLE 主鍵），建立後不可變更。</summary>
public class CreateRoleRequest
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? RoleType { get; set; }
    public string SystemCode { get; set; } = "PGM";
}

/// <summary>編輯角色請求。RoleId 由路由決定，不在此異動。</summary>
public class UpdateRoleRequest
{
    public string RoleName { get; set; } = string.Empty;
    public string? RoleType { get; set; }
    public string SystemCode { get; set; } = "PGM";
}

/// <summary>啟用/停用角色請求（對應 DEL_FLG 反轉）。</summary>
public class RoleStatusRequest
{
    public bool IsActive { get; set; }
}

/// <summary>供角色授權勾選的功能項目（來自 dbo.SysFun）。</summary>
public class RoleFunctionDto
{
    public string FunctionId { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string? FunctionUrl { get; set; }
    public string? ParentId { get; set; }
    public decimal SortId { get; set; }

    /// <summary>此角色目前是否已授權該功能。</summary>
    public bool Granted { get; set; }
}

/// <summary>角色功能權限畫面資料：全部啟用中功能 + 該角色目前的授權狀態。</summary>
public class RolePermissionsDto
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public IReadOnlyList<RoleFunctionDto> Functions { get; set; } = [];
}

/// <summary>儲存角色功能權限：以勾選的 FunctionId 全量取代該角色授權。</summary>
public class SaveRolePermissionsRequest
{
    public IReadOnlyList<string> FunctionIds { get; set; } = [];
}
