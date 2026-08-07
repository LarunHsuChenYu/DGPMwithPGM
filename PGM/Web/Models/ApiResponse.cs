namespace PGM.Web.Models;

/// <summary>
/// 對應後端 Core 的 ApiResponse&lt;T&gt; transport contract（Code / Message / TraceId / Data）。
/// Web 不 ProjectReference Core，故於此複製傳輸結構；欄位異動時需與 Api 同步。
/// </summary>
public class ApiResponse<T>
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public T? Data { get; set; }
}

public static class ApiResponseCodes
{
    /// <summary>後端 ErrorCodes.Success 的 code。</summary>
    public const string Success = "100";
}
