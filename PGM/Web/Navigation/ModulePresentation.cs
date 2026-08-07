namespace PGM.Web.Navigation;

/// <summary>首頁／模組概覽卡片的顯示資訊（依 SysFun 模組 Fun_ID）。</summary>
public static class ModulePresentation
{
    public static (string Abbr, string Color, string Description) For(string? functionId) =>
        functionId switch
        {
            "Permission" => ("權", "#7c3aed", "功能、角色與使用者帳號管理"),
            "SysConfig" => ("參", "#0d9488", "系統代碼／參數維護"),
            "Syslog" => ("查", "#475569", "登入歷程等系統查詢"),
            _ => (
                string.IsNullOrWhiteSpace(functionId) ? "模" : functionId[..1].ToUpperInvariant(),
                "#64748b",
                "請選擇子功能進入")
        };

    public static string OverviewUrl(string? functionId) =>
        $"/module/{Uri.EscapeDataString(functionId ?? string.Empty)}";
}
