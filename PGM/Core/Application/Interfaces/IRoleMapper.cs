using PGM.Core.Application.Models.RoleManagement;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Application.Interfaces;

public interface IRoleMapper
{
    RoleDto ToDto(Role role);
    IReadOnlyList<RoleDto> ToDtos(IEnumerable<Role> roles);
    RoleFunctionDto ToFunctionDto(SysFun entity);
    IReadOnlyList<RoleFunctionDto> ToFunctionDtos(IEnumerable<SysFun> entities);
}
