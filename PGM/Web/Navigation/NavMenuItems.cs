using PGM.Web.Models;

namespace PGM.Web.Navigation;

/// <summary>
/// 側邊選單節點。由 /api/auth/menus（SET_FUNCTION：Is_Menu=Y、Is_Enabled=Y）動態組樹，
/// 不以硬編碼 Fun_ID 清單決定可見項目。
/// </summary>
public record NavMenuItem(
    string Title,
    string? Url,
    string? FunctionId = null,
    IReadOnlyList<NavMenuItem>? Children = null);

public static class NavMenuItems
{
    /// <summary>
    /// 將扁平 menus 組為側欄節點。
    /// 規則：
    /// - ParentId 空白且有子項 → 群組（標題＋展開子連結）
    /// - ParentId 空白且有 Url、無子項 → 扁平頂層連結（對齊設計稿側欄）
    /// - 群組下無可見子項且自身無 Url → 不顯示
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
            if (byParent.TryGetValue(root.FunctionId, out var children) && children.Count > 0)
            {
                result.Add(new NavMenuItem(
                    root.FunctionName,
                    Url: null,
                    root.FunctionId,
                    children.Select(c => new NavMenuItem(
                        c.FunctionName,
                        NormalizeUrl(c.FunctionUrl),
                        c.FunctionId)).ToList()));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(root.FunctionUrl))
            {
                result.Add(new NavMenuItem(
                    root.FunctionName,
                    NormalizeUrl(root.FunctionUrl),
                    root.FunctionId));
            }
        }

        return result;
    }

    private static string NormalizeUrl(string? url)
    {
        var value = (url ?? string.Empty).Trim();
        if (value.Length == 0)
            return "/";
        return value.StartsWith('/') ? value : "/" + value;
    }
}
