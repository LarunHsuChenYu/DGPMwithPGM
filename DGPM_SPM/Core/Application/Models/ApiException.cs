namespace DGPM_SPM.Core.Application.Models;

public class ApiException : Exception
{
    public string ErrorCode { get; }
    public int StatusCode { get; }

    public ApiException(string errorCode, string message, int statusCode = 400)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
