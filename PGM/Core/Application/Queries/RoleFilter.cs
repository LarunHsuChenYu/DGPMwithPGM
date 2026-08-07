namespace PGM.Core.Application.Queries;

/// <summary>角色列表查詢條件（分頁由 FilterBase 提供）。</summary>
public class RoleFilter : FilterBase
{
    /// <summary>模糊比對角色代碼或名稱。</summary>
    public string? Keyword { get; set; }

    /// <summary>true=啟用、false=停用、null=不限（對應 DEL_FLG 反轉）。</summary>
    public bool? IsActive { get; set; }
}
