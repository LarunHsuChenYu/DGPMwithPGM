namespace PGM.Core.Application.Models.Parameter;

/// <summary>相容讀取用 DTO（GET /api/parameters/{setItem}）。</summary>
public class ParameterItemDto
{
    public string SetItem { get; set; } = string.Empty;
    public string SetId { get; set; } = string.Empty;
    public string SetValue { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

/// <summary>ParamSet 類別下拉（SET_PARAMITEM，只讀）。</summary>
public class ParameterCategoryDto
{
    public string SetItem { get; set; } = string.Empty;
    public string SetItemName { get; set; } = string.Empty;
}

/// <summary>ParamSet Grid／編輯回傳列。</summary>
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
