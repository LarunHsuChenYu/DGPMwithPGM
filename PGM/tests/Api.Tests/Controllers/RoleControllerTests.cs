using PGM.Api.Controllers;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Models;
using PGM.Core.Application.Models.Api.Response;
using PGM.Core.Application.Models.RoleManagement;
using PGM.Core.Application.Queries;

namespace PGM.Api.Tests.Controllers;

public class RoleControllerTests
{
    private readonly IRoleService _service = Substitute.For<IRoleService>();
    private readonly RoleController _sut;

    public RoleControllerTests()
    {
        _sut = new RoleController(_service);
    }

    [Fact]
    public async Task GetPaged_Always_ReturnsOk()
    {
        var paged = new PagedResult<RoleDto> { Datas = new List<RoleDto>(), TotalRow = 0, Page = 1, PageSize = 20 };
        _service.GetPagedAsync(Arg.Any<RoleFilter>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<PagedResult<RoleDto>>.SuccessResult(paged));

        var result = await _sut.GetPaged(new RoleFilter(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Always_ReturnsOk()
    {
        _service.GetByIdAsync("R1", Arg.Any<CancellationToken>())
            .Returns(ApiResponse<RoleDto?>.SuccessResult(new RoleDto()));

        var result = await _sut.GetById("R1", CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Create_Always_ReturnsOk()
    {
        _service.CreateAsync(Arg.Any<CreateRoleRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<RoleDto?>.SuccessResult(new RoleDto()));

        var result = await _sut.Create(new CreateRoleRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_Always_ReturnsOk()
    {
        _service.UpdateAsync("R1", Arg.Any<UpdateRoleRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<RoleDto?>.SuccessResult(new RoleDto()));

        var result = await _sut.Update("R1", new UpdateRoleRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_Always_ReturnsOk()
    {
        _service.UpdateStatusAsync("R1", Arg.Any<RoleStatusRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<bool>.SuccessResult(true));

        var result = await _sut.UpdateStatus("R1", new RoleStatusRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPermissions_Always_ReturnsOk()
    {
        _service.GetPermissionsAsync("R1", Arg.Any<CancellationToken>())
            .Returns(ApiResponse<RolePermissionsDto?>.SuccessResult(new RolePermissionsDto()));

        var result = await _sut.GetPermissions("R1", CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SavePermissions_Always_ReturnsOk()
    {
        _service.SavePermissionsAsync("R1", Arg.Any<SaveRolePermissionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiResponse<bool>.SuccessResult(true));

        var result = await _sut.SavePermissions("R1", new SaveRolePermissionsRequest(), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
    }
}
