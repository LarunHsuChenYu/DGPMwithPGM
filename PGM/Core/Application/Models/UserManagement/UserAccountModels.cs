namespace PGM.Core.Application.Models.UserManagement;

/// <summary>使用者帳號列表與明細 DTO；不包含密碼雜湊。</summary>
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

/// <summary>新增帳號請求。初始密碼只用於 BCrypt 雜湊，不會儲存或回傳明碼。</summary>
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

/// <summary>編輯帳號基本資料與角色；帳號及密碼不在此流程異動。</summary>
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
