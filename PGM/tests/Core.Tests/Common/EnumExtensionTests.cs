using PGM.Core.Application.Models.Enums;
using PGM.Core.Common.Extensions;

namespace PGM.Core.Tests.Common;

public class EnumExtensionTests
{
    [Theory]
    [InlineData(ErrorCodes.Success, "code", "100")]
    [InlineData(ErrorCodes.Success, "message", "Success")]
    [InlineData(ErrorCodes.InternalError, "code", "9999")]
    [InlineData(ErrorCodes.UnauthorizedAccess, "message", "Unauthorized access")]
    public void GetDescription_ReturnsCategoryValue(ErrorCodes value, string category, string expected)
    {
        value.GetDescription(category).ShouldBe(expected);
    }

    [Fact]
    public void GetDescription_FallsBackToName_WhenCategoryMissing()
    {
        ErrorCodes.Success.GetDescription("not-exist").ShouldBe(nameof(ErrorCodes.Success));
    }

    [Fact]
    public void ToUnderlyingString_ReturnsUnderlyingValue()
    {
        ErrorCodes.InternalError.ToUnderlyingString().ShouldBe("9999");
    }

    [Fact]
    public void GetAllDescriptions_ReturnsAllCategories()
    {
        var all = ErrorCodes.Success.GetAllDescriptions().ToList();

        all.Count.ShouldBe(2);
        all.ShouldContain(a => a.Category == "code" && a.Description == "100");
        all.ShouldContain(a => a.Category == "message" && a.Description == "Success");
    }
}
