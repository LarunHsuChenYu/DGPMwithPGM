using Microsoft.Extensions.Caching.Memory;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Mapping;
using DGPM_SPM.Core.Application.Models.Api.Response;
using DGPM_SPM.Core.Application.Models.Parameter;
using DGPM_SPM.Core.Common.Attributes;

namespace DGPM_SPM.Core.Application.Services;

/// <summary>
/// 對齊 QMS ParameterService.GetParameterList（MemoryCache Key: SetParam_{setItem}）。
/// </summary>
[ScopedRegistration]
public class ParameterService : IParameterService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;
    private readonly IParameterMapper _parameterMapper;
    private readonly IRequestContext _requestContext;

    public ParameterService(
        IUnitOfWork unitOfWork,
        IMemoryCache cache,
        IParameterMapper parameterMapper,
        IRequestContext requestContext)
    {
        _unitOfWork = unitOfWork;
        _cache = cache;
        _parameterMapper = parameterMapper;
        _requestContext = requestContext;
    }

    public async Task<ApiResponse<List<ParameterItemDto>>> GetParameterListAsync(
        string setItem,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(setItem))
        {
            return ApiResponse<List<ParameterItemDto>>.SuccessResult([], traceId: _requestContext.TraceId);
        }

        var cacheKey = $"SetParam_{setItem}";
        if (_cache.TryGetValue(cacheKey, out List<ParameterItemDto>? cached) && cached is not null)
        {
            return ApiResponse<List<ParameterItemDto>>.SuccessResult(cached, traceId: _requestContext.TraceId);
        }

        var entities = await _unitOfWork.Parameters.GetAllByItemAsync(setItem, ct);
        var list = _parameterMapper.ToDtos(entities).ToList();

        _cache.Set(cacheKey, list, TimeSpan.FromHours(6));
        return ApiResponse<List<ParameterItemDto>>.SuccessResult(list, traceId: _requestContext.TraceId);
    }
}
