using DGPM_SPM.Core.Application.Models.Parameter;
using DGPM_SPM.Core.Common.Attributes;
using DGPM_SPM.Core.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace DGPM_SPM.Core.Application.Mapping;

public interface IParameterMapper
{
    ParameterItemDto ToDto(Parameter parameter);
    IReadOnlyList<ParameterItemDto> ToDtos(IEnumerable<Parameter> parameters);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
[ScopedRegistration]
public partial class ParameterMapper : IParameterMapper
{
    public partial ParameterItemDto ToDto(Parameter parameter);

    public partial IReadOnlyList<ParameterItemDto> ToDtos(IEnumerable<Parameter> parameters);
}
