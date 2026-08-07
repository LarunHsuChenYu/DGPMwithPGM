namespace PGM.Core.Domain.Entities;

/// <summary>dbo.SET_PARAM — 參數類主檔細項（複合鍵 SET_ITEM + SET_ID）。</summary>
public class Parameter : BaseEntity
{
    public string SetItem { get; set; } = string.Empty;
    public string SetId { get; set; } = string.Empty;
    public string SetValue { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? Memo { get; set; }
    public bool DelFlg { get; set; }
}
