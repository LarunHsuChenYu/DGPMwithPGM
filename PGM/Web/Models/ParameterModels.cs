using System.ComponentModel.DataAnnotations;

namespace PGM.Web.Models;

public class ParameterCategoryDto
{
    public string SetItem { get; set; } = string.Empty;
    public string SetItemName { get; set; } = string.Empty;
}

public class ParameterDto
{
    public string SetItem { get; set; } = string.Empty;
    public string SetItemName { get; set; } = string.Empty;
    public string SetId { get; set; } = string.Empty;
    public string SetValue { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class CreateParameterRequest
{
    public string SetItem { get; set; } = string.Empty;
    public string SetId { get; set; } = string.Empty;
    public string SetValue { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class UpdateParameterRequest
{
    public string SetValue { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class ParameterEditModel
{
    public string SetItem { get; set; } = string.Empty;
    public string SetItemName { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入代碼")]
    [StringLength(20, ErrorMessage = "代碼不可超過 20 字元")]
    public string SetId { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入代碼名稱")]
    [StringLength(50, ErrorMessage = "代碼名稱不可超過 50 字元")]
    public string SetValue { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入排序")]
    [Range(0, int.MaxValue, ErrorMessage = "排序須為非負整數")]
    public int SortOrder { get; set; }
}
