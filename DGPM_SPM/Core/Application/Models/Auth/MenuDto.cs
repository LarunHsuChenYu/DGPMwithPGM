namespace DGPM_SPM.Core.Application.Models.Auth;

public class MenuDto
{
    public string FunctionId { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string? FunctionUrl { get; set; }
    public string? ParentId { get; set; }
    /// <summary>對應 SysFun.Sort_Order。</summary>
    public decimal SortId { get; set; }
}
