namespace DGPM_SPM.Core.Application.Models.Api.Response;

public class ApiResponse<T>
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string code = "100", string message = "Success", string traceId = "")
        => new() { Code = code, Data = data, Message = message, TraceId = traceId };

    public static ApiResponse<T> ErrorResult(string errorCode, string message, string traceId = "")
        => new() { Code = errorCode, Message = message, TraceId = traceId };
}
