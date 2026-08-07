namespace PGM.Core.Common.Security;

/// <summary>
/// EMPSet 新增帳號預設密碼，以及首次登入強制改密判定（domain Login FORCE_PWD）。
/// </summary>
public static class DefaultPassword
{
    public const string Value = "0000";

    public static bool IsDefaultPlain(string? plainPassword) =>
        string.Equals(plainPassword?.Trim(), Value, StringComparison.Ordinal);

    /// <summary>以 BCrypt Verify 判斷是否仍為預設密碼（禁止 SQL 明文等值）。</summary>
    public static bool IsDefaultHash(string? passwordHash) =>
        !string.IsNullOrWhiteSpace(passwordHash)
        && BCrypt.Net.BCrypt.Verify(Value, passwordHash);
}
