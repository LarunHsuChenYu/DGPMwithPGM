using DGPM_SPM.Core.Application.Models.Auth;
using DGPM_SPM.Core.Common.Attributes;
using DGPM_SPM.Core.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace DGPM_SPM.Core.Application.Mapping;

public interface IAuthMapper
{
    MenuDto ToMenuDto(SysFun entity);
    IReadOnlyList<MenuDto> ToMenuDtos(IEnumerable<SysFun> entities);
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
[ScopedRegistration]
public partial class AuthMapper : IAuthMapper
{
    [MapProperty(nameof(SysFun.FunId), nameof(MenuDto.FunctionId))]
    [MapProperty(nameof(SysFun.FunName), nameof(MenuDto.FunctionName))]
    [MapProperty(nameof(SysFun.UrlPath), nameof(MenuDto.FunctionUrl))]
    [MapProperty(nameof(SysFun.SortOrder), nameof(MenuDto.SortId))]
    public partial MenuDto ToMenuDto(SysFun entity);

    public partial IReadOnlyList<MenuDto> ToMenuDtos(IEnumerable<SysFun> entities);
}
