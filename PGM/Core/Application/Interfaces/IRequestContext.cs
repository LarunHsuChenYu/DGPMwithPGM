namespace PGM.Core.Application.Interfaces;

/// <summary>
/// 抽象化的請求上下文。實作放在 Api 層（HttpContext 是 HTTP 細節，不屬於 Core）。
/// </summary>
public interface IRequestContext
{
    string TraceId { get; }
    string GetTraceId();
}
