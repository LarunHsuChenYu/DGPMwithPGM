using PGM.Core.Application.Models.Parameter;
using PGM.Core.Common.Attributes;
using PGM.Core.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace PGM.Core.Application.Mapping;

public interface IParameterMapper
{
    ParameterItemDto ToItemDto(Parameter parameter);
    IReadOnlyList<ParameterItemDto> ToItemDtos(IEnumerable<Parameter> parameters);
    ParameterCategoryDto ToCategoryDto(ParamItem item);
    IReadOnlyList<ParameterCategoryDto> ToCategoryDtos(IEnumerable<ParamItem> items);
    ParameterDto ToDto(Parameter parameter, string setItemName);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
[ScopedRegistration]
public partial class ParameterMapper : IParameterMapper
{
    public partial ParameterItemDto ToItemDto(Parameter parameter);

    public partial IReadOnlyList<ParameterItemDto> ToItemDtos(IEnumerable<Parameter> parameters);

    public partial ParameterCategoryDto ToCategoryDto(ParamItem item);

    public partial IReadOnlyList<ParameterCategoryDto> ToCategoryDtos(IEnumerable<ParamItem> items);

    [MapperIgnoreTarget(nameof(ParameterDto.SetItemName))]
    public partial ParameterDto ToDtoCore(Parameter parameter);

    public ParameterDto ToDto(Parameter parameter, string setItemName)
    {
        var dto = ToDtoCore(parameter);
        dto.SetItemName = setItemName;
        return dto;
    }
}
