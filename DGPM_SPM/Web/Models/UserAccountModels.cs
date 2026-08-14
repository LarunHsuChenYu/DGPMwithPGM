using System.ComponentModel.DataAnnotations;

namespace DGPM_SPM.Web.Models;

public class UserAccountDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? TitName { get; set; }
    public string? Email { get; set; }
    public string? Telephone { get; set; }
    public string? FactoryNo { get; set; }
    public string? DptCode { get; set; }
    public bool? DelFlg { get; set; }
    public IReadOnlyList<RoleOptionDto> Roles { get; set; } = [];
    public DateTime? CrtDate { get; set; }
    public string? CrtUser { get; set; }
    public DateTime? MdfDate { get; set; }
    public string? MdfUser { get; set; }
}

public class RoleOptionDto
{
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}

public class UserAccountEditModel
{
    [Required(ErrorMessage = "請輸入使用者帳號")]
    [StringLength(10, ErrorMessage = "使用者帳號不可超過 10 字元")]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入姓名")]
    [StringLength(100, ErrorMessage = "姓名不可超過 100 字元")]
    public string UserName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email 格式不正確")]
    [StringLength(200, ErrorMessage = "Email 不可超過 200 字元")]
    public string? Email { get; set; }

    [StringLength(50, ErrorMessage = "電話不可超過 50 字元")]
    public string? Telephone { get; set; }

    /// <summary>畫面單一角色下拉；儲存時組成 RoleIds。</summary>
    public string SelectedRoleId { get; set; } = string.Empty;
}

public class CreateUserAccountRequest
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string InitialPassword { get; set; } = string.Empty;
    public string? TitName { get; set; }
    public string? Email { get; set; }
    public string? Telephone { get; set; }
    public string? FactoryNo { get; set; }
    public string? DptCode { get; set; }
    public IReadOnlyList<string> RoleIds { get; set; } = [];
}

public class UpdateUserAccountRequest
{
    public string UserName { get; set; } = string.Empty;
    public string? TitName { get; set; }
    public string? Email { get; set; }
    public string? Telephone { get; set; }
    public string? FactoryNo { get; set; }
    public string? DptCode { get; set; }
    public IReadOnlyList<string> RoleIds { get; set; } = [];
}

public class UserAccountStatusRequest
{
    public bool IsActive { get; set; }
}

