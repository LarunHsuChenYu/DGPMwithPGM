namespace PGM.Core.Common.Auth;

/// <summary>
/// 系統權限 UI 所在端（單一真相＝SET_PARAM）。
/// On＝在 PGM Web；Off＝在 DGPM（轉發 PGM AUTH API）。
/// </summary>
public static class PgmUiMode
{
    public const string SetItem = "Auth";
    public const string SetId = "PgmUiMode";
    public const string On = "On";
    public const string Off = "Off";
    public const string Default = On;

    public const string DgpmAuthParentId = "Permission";
    public const string DgpmAuthParentName = "系統管理權限";

    /// <summary>可切 PgmUiMode 的角色：<c>PGMAdmin</c>（現況）或舊種子 <c>ADMIN</c>。</summary>
    public const string PgmAdminRoleId = "PGMAdmin";

    public const string LegacyAdminRoleId = "ADMIN";

    /// <summary>
    /// JWT／MAP 的 ROLE_ID 是否可切 Mode（帳號不硬編碼；AshtonHsu 與 Admin 只要掛這些角色即可）。
    /// </summary>
    public static bool IsModeToggleRole(string? roleId)
    {
        if (string.IsNullOrWhiteSpace(roleId))
            return false;
        var plain = roleId.Split('$')[0];
        return string.Equals(plain, PgmAdminRoleId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(plain, LegacyAdminRoleId, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsOn(string? value) =>
        string.Equals(Normalize(value), On, StringComparison.OrdinalIgnoreCase);

    public static bool IsOff(string? value) =>
        string.Equals(Normalize(value), Off, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? value)
    {
        if (string.Equals(value?.Trim(), Off, StringComparison.OrdinalIgnoreCase))
            return Off;
        return On;
    }

    public static bool IsAuthFunctionId(string? functionId) =>
        !string.IsNullOrWhiteSpace(functionId)
        && functionId.StartsWith("AUTH", StringComparison.OrdinalIgnoreCase);

    /// <summary>Mode=Off 時 DGPM 選單 URL（與 PGM 頁路徑對齊，供 DGPM Blazor 承接）。</summary>
    public static string? ResolveDgpmAuthUrl(string functionId) => functionId.ToUpperInvariant() switch
    {
        "AUTH01" => "/system/users",
        "AUTH02" => "/system/roles",
        "AUTH03" => "/account/change-password",
        "AUTH04" => "/parameters/param-set",
        "AUTH06" => "/system/functions",
        "AUTH07" => "/system/role-master",
        "AUTH08" => "/query/login-history",
        "AUTH09" => null, // 能力 Fun；操作掛在帳號維護頁
        _ => null
    };
}
