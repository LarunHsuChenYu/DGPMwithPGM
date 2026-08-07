using PGM.Core.Application.Models.Auth;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Application.Interfaces;

public interface IAuthenticationLogMapper
{
    AuthenticationLogDto ToDto(AuthenticationLog authenticationLog);
    IReadOnlyList<AuthenticationLogDto> ToDtos(IEnumerable<AuthenticationLog> authenticationLogs);
}
