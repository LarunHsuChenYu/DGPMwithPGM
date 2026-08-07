namespace PGM.Core.Domain.Entities;

/// <summary>dbo.SET_PARAMITEM — 參數類別主檔（畫面只讀）。</summary>
public class ParamItem : BaseEntity
{
    public string SetItem { get; set; } = string.Empty;
    public string SetItemName { get; set; } = string.Empty;
    public string? Memo { get; set; }
    public bool DelFlg { get; set; }
}
