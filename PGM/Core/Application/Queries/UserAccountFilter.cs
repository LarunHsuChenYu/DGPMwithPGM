namespace PGM.Core.Application.Queries;

/// <summary>使用者帳號列表查詢條件（分頁由 FilterBase 提供）。</summary>
public class UserAccountFilter : FilterBase
{
    /// <summary>模糊比對帳號、姓名、Email 或部門代碼。</summary>
    public string? Keyword { get; set; }

    /// <summary>true=啟用、false=停用、null=不限。</summary>
    public bool? IsActive { get; set; }

    /// <summary>指定角色。</summary>
    public string? RoleId { get; set; }
}
