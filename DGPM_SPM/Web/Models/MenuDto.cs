namespace DGPM_SPM.Web.Models;

/// <summary>對應 GET /api/auth/menus 與 LoginResponse.Menus 的選單項目。</summary>
public class MenuDto
{
    public string FunctionId { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string? FunctionUrl { get; set; }
    public string? ParentId { get; set; }
    public decimal SortId { get; set; }
}
