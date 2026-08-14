using PGM.Core.Common.Auth;

namespace PGM.Core.Tests.Common;

public class PgmUiModeTests
{
    [Theory]
    [InlineData("PGMAdmin")]
    [InlineData("pgmadmin")]
    [InlineData("PGMAdmin$AshtonHsu$SELF")]
    [InlineData("ADMIN")]
    [InlineData("ADMIN$Admin$SELF")]
    public void IsModeToggleRole_AllowsPgmAdminAndLegacyAdmin(string roleId)
        => PgmUiMode.IsModeToggleRole(roleId).ShouldBeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("DGPMAdmin")]
    [InlineData("DGPMAdmin$CathyWang$SELF")]
    [InlineData("DGPMUploader")]
    public void IsModeToggleRole_RejectsNonAdminRoles(string? roleId)
        => PgmUiMode.IsModeToggleRole(roleId).ShouldBeFalse();
}
