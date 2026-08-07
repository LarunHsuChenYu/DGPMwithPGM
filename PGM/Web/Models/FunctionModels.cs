using System.ComponentModel.DataAnnotations;

namespace PGM.Web.Models;

public class FunctionDto
{
    public string FunId { get; set; } = string.Empty;
    public string FunName { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string? ParentName { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string? UrlPath { get; set; }
    public decimal SortOrder { get; set; }
    public string IsMenu { get; set; } = "N";
    public string IsEnabled { get; set; } = "N";
    public string? FunDesc { get; set; }
    public DateTime CreDate { get; set; }
    public string CrePerson { get; set; } = string.Empty;
    public DateTime ChgDate { get; set; }
    public string ChgPerson { get; set; } = string.Empty;
}

public class FunctionOptionDto
{
    public string FunId { get; set; } = string.Empty;
    public string FunName { get; set; } = string.Empty;
    /// <summary>對應 SysFun.Sort_Order；供建議子層序號前綴（側欄歸屬仍依 ParentId）。</summary>
    public decimal SortOrder { get; set; }
}

public class SaveFunctionRequest
{
    [Required(ErrorMessage = "請輸入功能代碼")]
    [StringLength(20, ErrorMessage = "功能代碼不可超過 20 字元")]
    public string FunId { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入功能名稱")]
    [StringLength(50, ErrorMessage = "功能名稱不可超過 50 字元")]
    public string FunName { get; set; } = string.Empty;

    public string? ParentId { get; set; }

    [Required(ErrorMessage = "請選擇功能類型")]
    public string ActionType { get; set; } = "P";

    [StringLength(50, ErrorMessage = "前端路由或 URL 不可超過 50 字元")]
    public string? UrlPath { get; set; }

    [Range(0, 9999.99, ErrorMessage = "階層序號超出允許範圍")]
    public decimal SortOrder { get; set; }

    [Required(ErrorMessage = "請選擇選單否")]
    public string IsMenu { get; set; } = string.Empty;

    [Required(ErrorMessage = "請選擇啟用否")]
    public string IsEnabled { get; set; } = "N";

    [StringLength(500, ErrorMessage = "說明不可超過 500 字元")]
    public string? FunDesc { get; set; }
}
