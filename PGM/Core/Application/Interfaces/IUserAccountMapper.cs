using PGM.Core.Application.Models.UserManagement;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Application.Interfaces;

public interface IUserAccountMapper
{
    UserAccountDto ToDto(User user);
    IReadOnlyList<UserAccountDto> ToDtos(IEnumerable<User> users);
    RoleOptionDto ToRoleOptionDto(Role role);
    IReadOnlyList<RoleOptionDto> ToRoleOptionDtos(IEnumerable<Role> roles);
}
