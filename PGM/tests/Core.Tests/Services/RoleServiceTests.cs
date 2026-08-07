using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Mapping;
using PGM.Core.Application.Models;
using PGM.Core.Application.Models.RoleManagement;
using PGM.Core.Application.Queries;
using PGM.Core.Application.Services;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Tests.Services;

public class RoleServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRoleRepository _roleRepo = Substitute.For<IRoleRepository>();
    private readonly IMenuRepository _menuRepo = Substitute.For<IMenuRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly RoleService _sut;

    public RoleServiceTests()
    {
        _uow.Roles.Returns(_roleRepo);
        _uow.Menus.Returns(_menuRepo);
        _currentUser.UserId.Returns("admin");
        _requestContext.TraceId.Returns("role-trace");
        _sut = new RoleService(_uow, new RoleMapper(), _currentUser, _requestContext);
    }

    private static Role SampleRole(string roleId = "SALES") => new()
    {
        RoleId = roleId,
        RoleName = "業務人員",
        RoleType = "BIZ",
        DelFlg = false
    };

    private static IReadOnlyList<SysFun> ActiveFunctions() =>
    [
        new() { FunId = "SYS", FunName = "系統權限管理", ParentId = null, SortOrder = 1, IsEnabled = "Y", DelYn = "N" },
        new() { FunId = "SYS-ROLE", FunName = "角色權限維護", ParentId = "SYS", SortOrder = 1, IsEnabled = "Y", DelYn = "N" },
        new() { FunId = "KPI", FunName = "經銷商KPI管理", ParentId = null, SortOrder = 2, IsEnabled = "Y", DelYn = "N" }
    ];

    private static CreateRoleRequest ValidCreateRequest() => new()
    {
        RoleId = " SALES ",
        RoleName = " 業務人員 ",
        RoleType = "BIZ"
    };

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPage()
    {
        var filter = new RoleFilter { Page = 1, PageSize = 20 };
        _roleRepo.GetPagedAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Role>
            {
                Datas = [SampleRole()],
                TotalRow = 1,
                Page = 1,
                PageSize = 20
            });

        var result = await _sut.GetPagedAsync(filter);

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.Datas.Single().RoleId.ShouldBe("SALES");
        result.Data.Datas.Single().RoleName.ShouldBe("業務人員");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNullData()
    {
        _roleRepo.GetByIdAsync("NONE", Arg.Any<CancellationToken>()).Returns((Role?)null);

        var result = await _sut.GetByIdAsync("NONE");

        result.Code.ShouldBe("100");
        result.Data.ShouldBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_TrimsAndCommits()
    {
        _roleRepo.GetByIdAsync("SALES", Arg.Any<CancellationToken>()).Returns(SampleRole());

        var result = await _sut.CreateAsync(ValidCreateRequest());

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.RoleId.ShouldBe("SALES");
        await _uow.Received(1).BeginTransactionAsync(
            Arg.Any<System.Data.IsolationLevel>(),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _roleRepo.Received(1).AddAsync(
            Arg.Is<Role>(role =>
                role.RoleId == "SALES"
                && role.RoleName == "業務人員"
                && role.RoleType == "BIZ"
                && role.DelFlg == false
                && role.CrtUser == "admin"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenRoleIdExists_ReturnsValidationError()
    {
        _roleRepo.ExistsAsync("SALES", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(ValidCreateRequest());

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("已存在");
        await _roleRepo.DidNotReceive().AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRoleId_ReturnsValidationError()
    {
        var request = ValidCreateRequest();
        request.RoleId = "SALES 01";

        var result = await _sut.CreateAsync(request);

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("角色代碼");
        await _roleRepo.DidNotReceive().AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenRepositoryFails_RollsBack()
    {
        _roleRepo.AddAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new InvalidOperationException("db error"));

        await Should.ThrowAsync<InvalidOperationException>(() => _sut.CreateAsync(ValidCreateRequest()));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsValidationError()
    {
        _roleRepo.GetByIdAsync("NONE", Arg.Any<CancellationToken>()).Returns((Role?)null);

        var result = await _sut.UpdateAsync("NONE", new UpdateRoleRequest { RoleName = "新名稱" });

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("找不到");
        await _roleRepo.DidNotReceive().UpdateAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_CommitsChange()
    {
        _roleRepo.GetByIdAsync("SALES", Arg.Any<CancellationToken>()).Returns(SampleRole());

        var result = await _sut.UpdateAsync(
            "SALES",
            new UpdateRoleRequest { RoleName = " 新名稱 ", RoleType = null });

        result.Code.ShouldBe("100");
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _roleRepo.Received(1).UpdateAsync(
            Arg.Is<Role>(role =>
                role.RoleName == "新名稱"
                && role.RoleType == null
                && role.MdfUser == "admin"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStatusAsync_DisablesRoleAndCommits()
    {
        _roleRepo.GetByIdAsync("SALES", Arg.Any<CancellationToken>()).Returns(SampleRole());
        _roleRepo.UpdateStatusAsync(Arg.Any<Role>(), Arg.Any<CancellationToken>()).Returns(1);

        var result = await _sut.UpdateStatusAsync("SALES", new RoleStatusRequest { IsActive = false });

        result.Code.ShouldBe("100");
        result.Data.ShouldBeTrue();
        await _roleRepo.Received(1).UpdateStatusAsync(
            Arg.Is<Role>(role => role.DelFlg == true && role.MdfUser == "admin"),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPermissionsAsync_MarksGrantedFunctions()
    {
        _roleRepo.GetByIdAsync("SALES", Arg.Any<CancellationToken>()).Returns(SampleRole());
        _menuRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(ActiveFunctions());
        _roleRepo.GetGrantedFunctionIdsAsync("SALES", Arg.Any<CancellationToken>())
            .Returns(["sys-role"]);

        var result = await _sut.GetPermissionsAsync("SALES");

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.RoleId.ShouldBe("SALES");
        result.Data.Functions.Count.ShouldBe(3);
        result.Data.Functions.Single(f => f.FunctionId == "SYS-ROLE").Granted.ShouldBeTrue();
        result.Data.Functions.Where(f => f.FunctionId != "SYS-ROLE")
            .ShouldAllBe(f => f.Granted == false);
    }

    [Fact]
    public async Task GetPermissionsAsync_WhenRoleNotFound_ReturnsNullData()
    {
        _roleRepo.GetByIdAsync("NONE", Arg.Any<CancellationToken>()).Returns((Role?)null);

        var result = await _sut.GetPermissionsAsync("NONE");

        result.Code.ShouldBe("100");
        result.Data.ShouldBeNull();
    }

    [Fact]
    public async Task SavePermissionsAsync_WithValidFunctions_NormalizesAndCommits()
    {
        _roleRepo.GetByIdAsync("SALES", Arg.Any<CancellationToken>()).Returns(SampleRole());
        _menuRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(ActiveFunctions());

        var request = new SaveRolePermissionsRequest
        {
            FunctionIds = [" SYS ", "sys-role", "SYS-ROLE", ""]
        };

        var result = await _sut.SavePermissionsAsync("SALES", request);

        result.Code.ShouldBe("100");
        result.Data.ShouldBeTrue();
        await _uow.Received(1).BeginTransactionAsync(
            Arg.Any<System.Data.IsolationLevel>(),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _roleRepo.Received(1).ReplaceFunctionsAsync(
            "SALES",
            Arg.Is<IReadOnlyCollection<string>>(ids =>
                ids.Count == 2 && ids.Contains("SYS") && ids.Contains("sys-role")),
            "admin",
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavePermissionsAsync_WithUnknownFunction_ReturnsValidationError()
    {
        _roleRepo.GetByIdAsync("SALES", Arg.Any<CancellationToken>()).Returns(SampleRole());
        _menuRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(ActiveFunctions());

        var result = await _sut.SavePermissionsAsync(
            "SALES",
            new SaveRolePermissionsRequest { FunctionIds = ["UNKNOWN"] });

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("功能");
        await _roleRepo.DidNotReceive().ReplaceFunctionsAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Any<string>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavePermissionsAsync_WithEmptySelection_ClearsPermissions()
    {
        _roleRepo.GetByIdAsync("SALES", Arg.Any<CancellationToken>()).Returns(SampleRole());
        _menuRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(ActiveFunctions());

        var result = await _sut.SavePermissionsAsync(
            "SALES",
            new SaveRolePermissionsRequest { FunctionIds = [] });

        result.Code.ShouldBe("100");
        result.Data.ShouldBeTrue();
        await _roleRepo.Received(1).ReplaceFunctionsAsync(
            "SALES",
            Arg.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0),
            "admin",
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavePermissionsAsync_WhenRepositoryFails_RollsBack()
    {
        _roleRepo.GetByIdAsync("SALES", Arg.Any<CancellationToken>()).Returns(SampleRole());
        _menuRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(ActiveFunctions());
        _roleRepo.ReplaceFunctionsAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<string>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("db error"));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _sut.SavePermissionsAsync("SALES", new SaveRolePermissionsRequest { FunctionIds = ["SYS"] }));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
