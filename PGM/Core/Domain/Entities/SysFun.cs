namespace PGM.Core.Domain.Entities;

/// <summary>系統功能設定檔（dbo.SysFun，依 DGPM_TableList）。</summary>
public class SysFun
{
    public string FunId { get; set; } = string.Empty;
    public string FunName { get; set; } = string.Empty;

    /// <summary>上層選單功能代碼；本專案頂層僅 null（不用 '0'）；Action_Type=M 時亦為 null。</summary>
    public string? ParentId { get; set; }

    /// <summary>上層功能名稱（查詢 join 帶出；寫入不使用）。</summary>
    public string? ParentName { get; set; }

    /// <summary>M=標題、P=頁面、B=按鈕。</summary>
    public string ActionType { get; set; } = string.Empty;

    public string? UrlPath { get; set; }

    /// <summary>選單圖示代碼；本階段 UI 不維護。</summary>
    public string? Icon { get; set; }

    public decimal SortOrder { get; set; }

    /// <summary>Y/N：是否顯示於選單。</summary>
    public string IsMenu { get; set; } = "N";

    /// <summary>Y/N：是否啟用（預設 N）。</summary>
    public string IsEnabled { get; set; } = "N";

    public string? FunDesc { get; set; }

    /// <summary>系統隔離碼：PGM／DGPM。</summary>
    public string SystemCode { get; set; } = "PGM";

    /// <summary>Y/N：軟刪標記（預設 N）。</summary>
    public string DelYn { get; set; } = "N";

    public string CrePerson { get; set; } = string.Empty;
    public DateTime CreDate { get; set; }
    public string ChgPerson { get; set; } = string.Empty;
    public DateTime ChgDate { get; set; }
}
