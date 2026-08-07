using DGPM_SPM.Web.Models;

namespace DGPM_SPM.Web.Navigation;

/// <summary>
/// 側邊選單節點。由 /api/auth/menus（PGM 為真相）動態組樹，
/// 不以硬編碼 Fun_ID 名單決定可見項目。
/// </summary>
public record NavMenuItem(
    string Title,
    string? Url,
    string? FunctionId = null,
    IReadOnlyList<NavMenuItem>? Children = null,
    bool IsExternal = false);

public static class NavMenuItems
{
    /// <summary>約定標記：外連 PGM Web（設定 Auth:PgmWebBaseUrl）。</summary>
    public const string ExtPgmMarker = "ext:pgm";

    /// <summary>舊版標記（seed 遷移過渡期仍可辨識）。</summary>
    public const string LegacyExtPgmMarker = "external:pgm";

    /// <summary>
    /// 將扁平 menus 組為「上層標題 → 葉功能連結」樹。
    /// 規則：ParentId 空白為群組；有 Url 且 ParentId 對應群組者為子項；
    /// 群組下無可見子項則不顯示。
    /// </summary>
    public static IReadOnlyList<NavMenuItem> Build(IReadOnlyCollection<MenuDto>? menus)
    {
        if (menus is null || menus.Count == 0)
            return [];

        var roots = menus
            .Where(m => string.IsNullOrWhiteSpace(m.ParentId))
            .OrderBy(m => m.SortId)
            .ThenBy(m => m.FunctionId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var byParent = menus
            .Where(m => !string.IsNullOrWhiteSpace(m.ParentId)
                        && !string.IsNullOrWhiteSpace(m.FunctionUrl))
            .GroupBy(m => m.ParentId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(m => m.SortId)
                    .ThenBy(m => m.FunctionId, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        var result = new List<NavMenuItem>();
        foreach (var root in roots)
        {
            if (!byParent.TryGetValue(root.FunctionId, out var children) || children.Count == 0)
                continue;

            result.Add(new NavMenuItem(
                root.FunctionName,
                Url: null,
                root.FunctionId,
                children.Select(c =>
                {
                    var url = NormalizeUrl(c.FunctionUrl);
                    return new NavMenuItem(
                        c.FunctionName,
                        url,
                        c.FunctionId,
                        Children: null,
                        IsExternal: IsExternalLink(url));
                }).ToList()));
        }

        return result;
    }

    /// <summary>是否為外連標記或絕對 URL（勿當站內 NavLink）。</summary>
    public static bool IsExternalLink(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var value = url.Trim();
        return IsPgmPortalMarker(value)
               || value.StartsWith("ext:", StringComparison.OrdinalIgnoreCase)
               || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
               || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>將選單項解析為可點擊 href（外連 <c>ext:pgm</c> → PgmWebBaseUrl）。</summary>
    public static string ResolveHref(NavMenuItem item, string? pgmWebBaseUrl)
    {
        if (!item.IsExternal)
            return string.IsNullOrWhiteSpace(item.Url) ? "#" : item.Url!;

        return ResolveExternalHref(item.Url, pgmWebBaseUrl);
    }

    /// <summary>將 <c>ext:pgm</c>（或已是 http(s)）解析為可開新分頁的 href。</summary>
    public static string ResolveExternalHref(string? url, string? pgmWebBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "#";

        var value = url.Trim();
        if (IsPgmPortalMarker(value))
        {
            var baseUrl = (pgmWebBaseUrl ?? string.Empty).Trim().TrimEnd('/');
            return string.IsNullOrEmpty(baseUrl) ? "#" : baseUrl;
        }

        return value;
    }

    internal static string NormalizeUrl(string? url)
    {
        var value = (url ?? string.Empty).Trim();
        if (value.Length == 0)
            return "/";

        // 外連約定與絕對 URL 原樣保留（勿加前綴 /）
        if (IsExternalLink(value))
            return value;

        return value.StartsWith('/') ? value : "/" + value;
    }

    private static bool IsPgmPortalMarker(string value) =>
        string.Equals(value, ExtPgmMarker, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, LegacyExtPgmMarker, StringComparison.OrdinalIgnoreCase);
}
