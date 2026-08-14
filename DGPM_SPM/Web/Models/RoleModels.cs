using System.ComponentModel.DataAnnotations;

namespace DGPM_SPM.Web.Models;

public class RoleDto
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? RoleType { get; set; }
    public string SystemCode { get; set; } = "PGM";

    /// <summary>true=已停用（DEL_FLG=1）。</summary>
    public bool DelFlg { get; set; }

    public DateTime? CrtDate { get; set; }
    public string? CrtUser { get; set; }
    public DateTime? MdfDate { get; set; }
    public string? MdfUser { get; set; }
}

public class SaveRoleRequest
{
    [Required(ErrorMessage = "請輸入角色代碼")]
    [StringLength(50, ErrorMessage = "角色代碼不可超過 50 字")]
    [RegularExpression("^[A-Za-z0-9_-]+$", ErrorMessage = "角色代碼僅允許英數字、底線與連字號")]
    public string RoleId { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入角色名稱")]
    [StringLength(100, ErrorMessage = "角色名稱不可超過 100 字")]
    public string RoleName { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "角色類型不可超過 20 字")]
    public string? RoleType { get; set; }

    [Required(ErrorMessage = "請選擇系統代碼")]
    [StringLength(20)]
    public string SystemCode { get; set; } = "PGM";
}

public class RoleStatusRequest
{
    public bool IsActive { get; set; }
}

public class RoleFunctionDto
{
    public string FunctionId { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string? FunctionUrl { get; set; }
    public string? ParentId { get; set; }
    public decimal SortId { get; set; }
    public bool Granted { get; set; }
}

public class RolePermissionsDto
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public IReadOnlyList<RoleFunctionDto> Functions { get; set; } = [];
}

public class SaveRolePermissionsRequest
{
    public IReadOnlyList<string> FunctionIds { get; set; } = [];
}

