namespace PGM.Core.Application.Queries;

/// <summary>使用者登入軌跡查詢條件（分頁由 FilterBase 提供）。</summary>
public class AuthenticationLogFilter : FilterBase
{
    /// <summary>使用者帳號（USER_ID），模糊比對；null 表示不篩選。</summary>
    public string? Keyword { get; set; }

    /// <summary>登入日期起（含當日）；null 表示不篩選。</summary>
    public DateTime? LoginDateFrom { get; set; }

    /// <summary>登入日期迄（含當日）；null 表示不篩選。</summary>
    public DateTime? LoginDateTo { get; set; }

    /// <summary>登入狀態（I=登入中／尚未登出, O=已登出）；null 表示不篩選。</summary>
    public string? AuthStatus { get; set; }
}
