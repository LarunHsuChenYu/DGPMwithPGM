namespace PGM.Core.Application.Models.Functions;

/// <summary>建立／編輯系統功能（dbo.SysFun）。</summary>
public class SaveFunctionRequest
{
    /// <summary>功能代碼（Fun_ID）。建立後不可修改。</summary>
    public string FunId { get; set; } = string.Empty;

    public string FunName { get; set; } = string.Empty;

    /// <summary>上層選單；Action_Type=M 時後端會強制清為 null。</summary>
    public string? ParentId { get; set; }

    /// <summary>M / P / B。</summary>
    public string ActionType { get; set; } = string.Empty;

    public string? UrlPath { get; set; }
    public decimal SortOrder { get; set; }

    /// <summary>Y / N；SRS 預設空值，儲存前必選。</summary>
    public string IsMenu { get; set; } = string.Empty;

    /// <summary>Y / N；預設 N。</summary>
    public string IsEnabled { get; set; } = "N";

    public string? FunDesc { get; set; }
}
