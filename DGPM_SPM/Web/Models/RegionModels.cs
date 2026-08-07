using System.ComponentModel.DataAnnotations;

namespace DGPM_SPM.Web.Models;

public class RegionDto
{
    public int RegionId { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public int? ParentRegionId { get; set; }
    public string? ParentRegionName { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RegionOptionDto
{
    public int RegionId { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
}

public class RegionSaveRequest
{
    [Required(ErrorMessage = "請輸入區域代碼")]
    [StringLength(20, ErrorMessage = "區域代碼不可超過 20 字元")]
    public string RegionCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入區域名稱")]
    [StringLength(100, ErrorMessage = "區域名稱不可超過 100 字元")]
    public string RegionName { get; set; } = string.Empty;

    public int? ParentRegionId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "排序不可小於 0")]
    public int SortOrder { get; set; }
}

public class RegionStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Datas { get; set; } = [];
    public int TotalRow { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
}
