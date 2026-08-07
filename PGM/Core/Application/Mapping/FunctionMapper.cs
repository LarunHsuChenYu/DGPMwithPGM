using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.Functions;
using PGM.Core.Common.Attributes;
using PGM.Core.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace PGM.Core.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
[ScopedRegistration]
public partial class FunctionMapper : IFunctionMapper
{
    public partial FunctionDto ToDto(SysFun entity);

    public partial IReadOnlyList<FunctionDto> ToDtos(IEnumerable<SysFun> entities);

    public partial FunctionOptionDto ToOptionDto(SysFun entity);

    public partial IReadOnlyList<FunctionOptionDto> ToOptionDtos(IEnumerable<SysFun> entities);
}
