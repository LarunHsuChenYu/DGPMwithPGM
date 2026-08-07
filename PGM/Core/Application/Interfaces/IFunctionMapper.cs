using PGM.Core.Application.Models.Functions;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Application.Interfaces;

public interface IFunctionMapper
{
    FunctionDto ToDto(SysFun entity);
    IReadOnlyList<FunctionDto> ToDtos(IEnumerable<SysFun> entities);
    FunctionOptionDto ToOptionDto(SysFun entity);
    IReadOnlyList<FunctionOptionDto> ToOptionDtos(IEnumerable<SysFun> entities);
}
