namespace DGPM_SPM.Web.Navigation;

/// <summary>首頁／模組概覽卡片的顯示資訊（依 SysFun 模組 Fun_ID）。</summary>
public static class ModulePresentation
{
    public static (string Abbr, string Color, string Description) For(string? functionId) =>
        functionId switch
        {
            "Masterdata" => ("基", "#2563eb", "經銷商與區域基本資料維護"),
            "SysConfig" => ("參", "#0d9488", "匯率等系統參數維護"),
            "KPIIndicator" => ("K", "#d97706", "KPI 指標、匯入、審核與資料權限"),
            "Syslog" => ("查", "#475569", "KPI 異動、匯入紀錄與登入歷程查詢"),
            "Dashboard" => ("儀", "#dc2626", "經銷商績效總覽儀錶板"),
            /* 殘留／已退役模組 Fun_ID 仍可能出現在舊 session */
            "Permission" => ("權", "#7c3aed", "已退役；帳號與角色請至 PGM"),
            _ => (
                string.IsNullOrWhiteSpace(functionId) ? "模" : functionId[..1].ToUpperInvariant(),
                "#64748b",
                "請選擇子功能進入")
        };

    public static string OverviewUrl(string? functionId) =>
        $"/module/{Uri.EscapeDataString(functionId ?? string.Empty)}";
}
