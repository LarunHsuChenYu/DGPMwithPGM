using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.RoleManagement;
using PGM.Core.Common.Attributes;
using PGM.Core.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace PGM.Core.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
[ScopedRegistration]
public partial class RoleMapper : IRoleMapper
{
    public partial RoleDto ToDto(Role role);

    public partial IReadOnlyList<RoleDto> ToDtos(IEnumerable<Role> roles);

    /// <summary>Granted 由 Service 依角色授權另行設定，非 SysFun 欄位。</summary>
    [MapProperty(nameof(SysFun.FunId), nameof(RoleFunctionDto.FunctionId))]
    [MapProperty(nameof(SysFun.FunName), nameof(RoleFunctionDto.FunctionName))]
    [MapProperty(nameof(SysFun.UrlPath), nameof(RoleFunctionDto.FunctionUrl))]
    [MapProperty(nameof(SysFun.SortOrder), nameof(RoleFunctionDto.SortId))]
    [MapperIgnoreTarget(nameof(RoleFunctionDto.Granted))]
    public partial RoleFunctionDto ToFunctionDto(SysFun entity);

    public partial IReadOnlyList<RoleFunctionDto> ToFunctionDtos(IEnumerable<SysFun> entities);
}
