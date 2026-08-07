using PGM.Api.Controllers;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.UserManagement;
using PGM.Core.Application.Queries;

namespace PGM.Api.Tests.Controllers;

public class UserAccountControllerTests
{
    private readonly IUserAccountService _service = Substitute.For<IUserAccountService>();
    private readonly UserAccountController _sut;

    public UserAccountControllerTests()
    {
        _sut = new UserAccountController(_service);
    }

    [Fact]
    public async Task GetPaged_Always_ReturnsOk()
    {
        var paged = new PagedResult<UserAccountDto> { Datas = new List<UserAccountDto>(), TotalRow = 0, Page = 1, PageSize = 20 };
        _service.GetPagedAsync(Arg.Any<UserAccountFilter>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<PagedResult<UserAccountDto>>.SuccessResult(paged));

        var result = await _sut.GetPaged(new UserAccountFilter(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetRoleOptions_Always_ReturnsOk()
    {
        _service.GetRoleOptionsAsync(Arg.Any<CancellationToken>())
            .Returns(ApiResponse<IReadOnlyList<RoleOptionDto>>.SuccessResult(new List<RoleOptionDto>()));

        var result = await _sut.GetRoleOptions(CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Always_ReturnsOk()
    {
        _service.GetByIdAsync("u1", Arg.Any<CancellationToken>())
            .Returns(ApiResponse<UserAccountDto?>.SuccessResult(new UserAccountDto()));

        var result = await _sut.GetById("u1", CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_Always_ReturnsOk()
    {
        _service.CreateAsync(Arg.Any<CreateUserAccountRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<UserAccountDto?>.SuccessResult(new UserAccountDto()));

        var result = await _sut.Create(new CreateUserAccountRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_Always_ReturnsOk()
    {
        _service.UpdateAsync("u1", Arg.Any<UpdateUserAccountRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<UserAccountDto?>.SuccessResult(new UserAccountDto()));

        var result = await _sut.Update("u1", new UpdateUserAccountRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_Always_ReturnsOk()
    {
        _service.UpdateStatusAsync("u1", Arg.Any<UserAccountStatusRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<bool>.SuccessResult(true));

        var result = await _sut.UpdateStatus("u1", new UserAccountStatusRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}
