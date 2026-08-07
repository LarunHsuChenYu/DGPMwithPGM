using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Mapping;
using PGM.Core.Application.Models;
using PGM.Core.Application.Models.UserManagement;
using PGM.Core.Application.Queries;
using PGM.Core.Application.Services;
using PGM.Core.Common.Security;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Tests.Services;

public class UserAccountServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepo = Substitute.For<IRoleRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly UserAccountService _sut;

    public UserAccountServiceTests()
    {
        _uow.Users.Returns(_userRepo);
        _uow.Roles.Returns(_roleRepo);
        _currentUser.UserId.Returns("admin");
        _requestContext.TraceId.Returns("user-trace");
        _roleRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(ActiveRoles());
        _sut = new UserAccountService(
            _uow,
            new UserAccountMapper(),
            _currentUser,
            _requestContext);
    }

    private static IReadOnlyList<Role> ActiveRoles() =>
    [
        new() { RoleId = "ADMIN", RoleName = "系統管理員" },
        new() { RoleId = "USER", RoleName = "一般使用者" }
    ];

    private static User SampleUser(string userId = "U001") => new()
    {
        UserId = userId,
        UserName = "測試人員",
        Password = "$2a$11$not-returned",
        DelFlg = false,
        Roles = [new Role { RoleId = "USER", RoleName = "一般使用者" }]
    };

    private static CreateUserAccountRequest ValidCreateRequest() => new()
    {
        UserId = " U001 ",
        UserName = " 測試人員 ",
        InitialPassword = "ValidPass123!",
        Email = "user@example.com",
        RoleIds = ["USER"]
    };

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedUsersWithoutPassword()
    {
        var filter = new UserAccountFilter { Page = 1, PageSize = 20 };
        _userRepo.GetPagedAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<User>
            {
                Datas = [SampleUser()],
                TotalRow = 1,
                Page = 1,
                PageSize = 20
            });

        var result = await _sut.GetPagedAsync(filter);

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.Datas.Single().UserId.ShouldBe("U001");
        result.Data.Datas.Single().Roles.Single().RoleName.ShouldBe("一般使用者");
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_HashesPasswordAndCommitsRoles()
    {
        _userRepo.GetForManagementAsync("U001", Arg.Any<CancellationToken>())
            .Returns(SampleUser());

        var result = await _sut.CreateAsync(ValidCreateRequest());

        result.Code.ShouldBe("100");
        await _uow.Received(1).BeginTransactionAsync(
            Arg.Any<System.Data.IsolationLevel>(),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _userRepo.Received(1).AddAsync(
            Arg.Is<User>(user =>
                user.UserId == "U001"
                && user.UserName == "測試人員"
                && user.Password != "ValidPass123!"
                && BCrypt.Net.BCrypt.Verify(DefaultPassword.Value, user.Password)
                && user.DelFlg == false
                && user.CrtUser == "admin"),
            Arg.Any<CancellationToken>());
        await _userRepo.Received(1).ReplaceRolesAsync(
            "U001",
            Arg.Is<IReadOnlyCollection<string>>(roles => roles.SequenceEqual(new[] { "USER" })),
            "admin",
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenUserExists_ReturnsValidationError()
    {
        _userRepo.ExistsAsync("U001", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(ValidCreateRequest());

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("已存在");
        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_IgnoresInitialPassword_AlwaysUsesDefault0000()
    {
        var request = ValidCreateRequest();
        request.InitialPassword = "short";
        _userRepo.GetForManagementAsync("U001", Arg.Any<CancellationToken>())
            .Returns(SampleUser());

        var result = await _sut.CreateAsync(request);

        result.Code.ShouldBe("100");
        await _userRepo.Received(1).AddAsync(
            Arg.Is<User>(user => BCrypt.Net.BCrypt.Verify(DefaultPassword.Value, user.Password)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenRoleIsInactive_ReturnsValidationError()
    {
        var request = ValidCreateRequest();
        request.RoleIds = ["REMOVED"];

        var result = await _sut.CreateAsync(request);

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("角色");
        await _userRepo.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenRepositoryFails_RollsBack()
    {
        _userRepo.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new InvalidOperationException("db error"));

        await Should.ThrowAsync<InvalidOperationException>(() => _sut.CreateAsync(ValidCreateRequest()));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesProfileAndRoles()
    {
        _userRepo.GetForManagementAsync("U001", Arg.Any<CancellationToken>())
            .Returns(SampleUser());
        var request = new UpdateUserAccountRequest
        {
            UserName = " 新姓名 ",
            DptCode = "IT",
            RoleIds = ["ADMIN", "USER"]
        };

        var result = await _sut.UpdateAsync("U001", request);

        result.Code.ShouldBe("100");
        await _userRepo.Received(1).UpdateAsync(
            Arg.Is<User>(user =>
                user.UserName == "新姓名"
                && user.DptCode == "IT"
                && user.MdfUser == "admin"),
            Arg.Any<CancellationToken>());
        await _userRepo.Received(1).ReplaceRolesAsync(
            "U001",
            Arg.Is<IReadOnlyCollection<string>>(roles => roles.Count == 2),
            "admin",
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStatusAsync_DisablesUserAndCommits()
    {
        _userRepo.GetForManagementAsync("U001", Arg.Any<CancellationToken>())
            .Returns(SampleUser());
        _userRepo.UpdateStatusAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(1);

        var result = await _sut.UpdateStatusAsync(
            "U001",
            new UserAccountStatusRequest { IsActive = false });

        result.Code.ShouldBe("100");
        result.Data.ShouldBeTrue();
        await _userRepo.Received(1).UpdateStatusAsync(
            Arg.Is<User>(user => user.DelFlg == true && user.MdfUser == "admin"),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
