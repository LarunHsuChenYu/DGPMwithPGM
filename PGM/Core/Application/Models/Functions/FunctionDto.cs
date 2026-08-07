namespace PGM.Core.Application.Models.Functions;

/// <summary>系統功能（dbo.SysFun）。</summary>
public class FunctionDto
{
    public string FunId { get; set; } = string.Empty;
    public string FunName { get; set; } = string.Empty;

    /// <summary>上層功能代碼；null = 最上層／標題。</summary>
    public string? ParentId { get; set; }
    public string? ParentName { get; set; }

    /// <summary>M=標題、P=頁面、B=按鈕。</summary>
    public string ActionType { get; set; } = string.Empty;

    public string? UrlPath { get; set; }
    public decimal SortOrder { get; set; }

    /// <summary>Y/N。</summary>
    public string IsMenu { get; set; } = "N";

    /// <summary>Y/N。</summary>
    public string IsEnabled { get; set; } = "N";

    public string? FunDesc { get; set; }

    public DateTime CreDate { get; set; }
    public string CrePerson { get; set; } = string.Empty;
    public DateTime ChgDate { get; set; }
    public string ChgPerson { get; set; } = string.Empty;
}

/// <summary>上層選單／父節點下拉選項。</summary>
public class FunctionOptionDto
{
    public string FunId { get; set; } = string.Empty;
    public string FunName { get; set; } = string.Empty;
    /// <summary>對應 SysFun.Sort_Order；供前端建議子層序號前綴（不影響側欄歸屬）。</summary>
    public decimal SortOrder { get; set; }
}
