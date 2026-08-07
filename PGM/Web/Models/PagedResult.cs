namespace PGM.Web.Models;

/// <summary>對應後端分頁信封；欄位需與 Api <c>PagedResult&lt;T&gt;</c> JSON 對齊。</summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Datas { get; set; } = [];
    public int TotalRow { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
}
