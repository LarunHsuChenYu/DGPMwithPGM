using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models.UserManagement;
using PGM.Core.Common.Attributes;
using PGM.Core.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace PGM.Core.Application.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
[ScopedRegistration]
public partial class UserAccountMapper : IUserAccountMapper
{
    public partial UserAccountDto ToDto(User user);
    public partial IReadOnlyList<UserAccountDto> ToDtos(IEnumerable<User> users);
    public partial RoleOptionDto ToRoleOptionDto(Role role);
    public partial IReadOnlyList<RoleOptionDto> ToRoleOptionDtos(IEnumerable<Role> roles);
}
