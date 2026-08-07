namespace DGPM_SPM.Web.Services;

/// <summary>
/// Web 端統一的 API 呼叫結果：成功時取 Data；失敗時 ErrorMessage 為可直接顯示的繁中友善訊息，
/// 不包含 stack trace、URL 或後端內部細節。IsUnauthorized 供呼叫端導向重新登入。
/// </summary>
public class ApiResult<T>
{
    public bool Succeeded { get; private init; }
    public T? Data { get; private init; }
    public string ErrorCode { get; private init; } = string.Empty;
    public string ErrorMessage { get; private init; } = string.Empty;
    public string TraceId { get; private init; } = string.Empty;
    /// <summary>HTTP 狀態碼；0 表示未取得回應（連線失敗／逾時）。</summary>
    public int HttpStatusCode { get; private init; }
    public bool IsUnauthorized { get; private init; }

    public static ApiResult<T> Ok(T? data) => new() { Succeeded = true, Data = data };

    public static ApiResult<T> Fail(
        string message,
        string errorCode = "",
        bool unauthorized = false,
        string? traceId = null,
        int httpStatusCode = 0) => new()
    {
        Succeeded = false,
        ErrorMessage = message,
        ErrorCode = errorCode,
        IsUnauthorized = unauthorized,
        TraceId = traceId ?? string.Empty,
        HttpStatusCode = httpStatusCode
    };
}
