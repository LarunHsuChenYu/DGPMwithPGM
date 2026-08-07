using System.Data;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Mapping;
using PGM.Core.Application.Models;
using PGM.Core.Application.Models.Functions;
using PGM.Core.Application.Queries;
using PGM.Core.Application.Services;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Tests.Services;

public class FunctionServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IFunctionRepository _repository = Substitute.For<IFunctionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly FunctionService _sut;

    public FunctionServiceTests()
    {
        _uow.Functions.Returns(_repository);
        _currentUser.UserId.Returns("tester");
        _requestContext.TraceId.Returns("function-trace");
        _sut = new FunctionService(_uow, new FunctionMapper(), _currentUser, _requestContext);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPage()
    {
        var filter = new FunctionFilter { Keyword = " 功能 ", ActionType = " p ", Page = 2, PageSize = 10 };
        _repository.GetPagedAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<SysFun>
            {
                Datas =
                [
                    new()
                    {
                        FunId = "FunctionList",
                        FunName = "系統功能管理",
                        ActionType = "P",
                        UrlPath = "/Permission/FunctionList",
                        SortOrder = 2.10m,
                        IsMenu = "Y",
                        IsEnabled = "Y",
                        DelYn = "N",
                        CrePerson = "SEED",
                        CreDate = DateTime.Today,
                        ChgPerson = "SEED",
                        ChgDate = DateTime.Today
                    }
                ],
                TotalRow = 12,
                Page = 2,
                PageSize = 10
            });

        var result = await _sut.GetPagedAsync(filter);

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.TotalRow.ShouldBe(12);
        result.Data.Datas[0].FunId.ShouldBe("FunctionList");
        result.Data.Datas[0].ActionType.ShouldBe("P");
        filter.Keyword.ShouldBe("功能");
        filter.ActionType.ShouldBe("P");
    }

    [Fact]
    public async Task GetPagedAsync_WithInvalidActionType_ReturnsInvalidParameter()
    {
        var result = await _sut.GetPagedAsync(new FunctionFilter { ActionType = "X" });

        result.Code.ShouldBe("200");
        await _repository.DidNotReceive().GetPagedAsync(
            Arg.Any<FunctionFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CommitsTransaction()
    {
        var request = new SaveFunctionRequest
        {
            FunId = " FunctionList ",
            FunName = " 系統功能管理 ",
            ActionType = "P",
            ParentId = "Permission",
            UrlPath = " /Permission/FunctionList ",
            SortOrder = 2.10m,
            IsMenu = "Y",
            IsEnabled = "Y"
        };
        _repository.ExistsFunIdAsync("FunctionList", Arg.Any<CancellationToken>()).Returns(false);
        _repository.GetByFunIdAsync("Permission", Arg.Any<CancellationToken>())
            .Returns(new SysFun { FunId = "Permission", FunName = "系統權限管理", ActionType = "M", DelYn = "N" });
        _repository.AddAsync(Arg.Any<SysFun>(), Arg.Any<CancellationToken>()).Returns(1);
        _repository.GetByFunIdAsync("FunctionList", Arg.Any<CancellationToken>())
            .Returns(new SysFun
            {
                FunId = "FunctionList",
                FunName = "系統功能管理",
                ParentId = "Permission",
                ActionType = "P",
                UrlPath = "/Permission/FunctionList",
                SortOrder = 2.10m,
                IsMenu = "Y",
                IsEnabled = "Y",
                DelYn = "N",
                CrePerson = "tester",
                CreDate = DateTime.Today,
                ChgPerson = "tester",
                ChgDate = DateTime.Today
            });

        var result = await _sut.CreateAsync(request);

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.FunId.ShouldBe("FunctionList");
        await _uow.Received(1).BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAsync(
            Arg.Is<SysFun>(x =>
                x.FunId == "FunctionList" &&
                x.FunName == "系統功能管理" &&
                x.ParentId == "Permission" &&
                x.ActionType == "P" &&
                x.UrlPath == "/Permission/FunctionList" &&
                x.DelYn == "N" &&
                x.CrePerson == "tester"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenActionTypeM_ClearsParentId()
    {
        _repository.ExistsFunIdAsync("Permission", Arg.Any<CancellationToken>()).Returns(false);
        _repository.AddAsync(Arg.Any<SysFun>(), Arg.Any<CancellationToken>()).Returns(1);
        _repository.GetByFunIdAsync("Permission", Arg.Any<CancellationToken>())
            .Returns(new SysFun
            {
                FunId = "Permission",
                FunName = "系統權限管理",
                ActionType = "M",
                ParentId = null,
                DelYn = "N",
                CrePerson = "tester",
                CreDate = DateTime.Today,
                ChgPerson = "tester",
                ChgDate = DateTime.Today
            });

        var result = await _sut.CreateAsync(new SaveFunctionRequest
        {
            FunId = "Permission",
            FunName = "系統權限管理",
            ActionType = "M",
            ParentId = "ShouldBeCleared",
            SortOrder = 2,
            IsMenu = "Y",
            IsEnabled = "Y"
        });

        result.Code.ShouldBe("100");
        await _repository.Received(1).AddAsync(
            Arg.Is<SysFun>(x => x.ParentId == null && x.ActionType == "M"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenParentIdIsZero_NormalizesToNull_AndRejectsNonModule()
    {
        // 非 M 時 '0' 正規化為 null → 上層必填失敗（拒絕寫入 '0'）
        var result = await _sut.CreateAsync(new SaveFunctionRequest
        {
            FunId = "FunctionList",
            FunName = "系統功能管理",
            ActionType = "P",
            ParentId = "0",
            SortOrder = 1,
            IsMenu = "Y",
            IsEnabled = "Y"
        });

        result.Code.ShouldNotBe("100");
        await _repository.DidNotReceive().AddAsync(Arg.Any<SysFun>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateFunId_ReturnsDuplicate()
    {
        _repository.ExistsFunIdAsync("FunctionList", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(new SaveFunctionRequest
        {
            FunId = "FunctionList",
            FunName = "系統功能管理",
            ActionType = "P",
            ParentId = "Permission",
            SortOrder = 1,
            IsMenu = "Y",
            IsEnabled = "Y"
        });

        result.Code.ShouldBe("409");
        await _repository.DidNotReceive().AddAsync(
            Arg.Any<SysFun>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFunIdChanged_ReturnsValidationError()
    {
        _repository.GetByFunIdAsync("OLD", Arg.Any<CancellationToken>())
            .Returns(new SysFun { FunId = "OLD", FunName = "舊功能", DelYn = "N" });

        var result = await _sut.UpdateAsync("OLD", new SaveFunctionRequest
        {
            FunId = "NEW",
            FunName = "新功能",
            ActionType = "M",
            SortOrder = 1,
            IsMenu = "Y",
            IsEnabled = "Y"
        });

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("不可修改");
        await _repository.DidNotReceive().UpdateAsync(
            Arg.Any<SysFun>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNoRowsAffected_ReturnsNotFound()
    {
        var existing = new SysFun
        {
            FunId = "FunctionList",
            FunName = "系統功能管理",
            ActionType = "P",
            ParentId = "SysConfig",
            DelYn = "N",
            IsMenu = "Y",
            IsEnabled = "Y",
            SortOrder = 1
        };
        _repository.GetByFunIdAsync("FunctionList", Arg.Any<CancellationToken>()).Returns(existing);
        _repository.GetByFunIdAsync("SysConfig", Arg.Any<CancellationToken>())
            .Returns(new SysFun { FunId = "SysConfig", FunName = "系統參數管理", DelYn = "N" });
        _repository.UpdateAsync(Arg.Any<SysFun>(), Arg.Any<CancellationToken>()).Returns(0);

        var result = await _sut.UpdateAsync("FunctionList", new SaveFunctionRequest
        {
            FunId = "FunctionList",
            FunName = "系統功能管理（改）",
            ActionType = "P",
            ParentId = "SysConfig",
            SortOrder = 1,
            IsMenu = "Y",
            IsEnabled = "Y",
            FunDesc = "test"
        });

        result.Code.ShouldBe("404");
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_CommitsAndReturnsDto()
    {
        var existing = new SysFun
        {
            FunId = "FunctionList",
            FunName = "系統功能管理",
            ActionType = "P",
            ParentId = "SysConfig",
            DelYn = "N",
            IsMenu = "Y",
            IsEnabled = "Y",
            SortOrder = 1
        };
        var updated = new SysFun
        {
            FunId = "FunctionList",
            FunName = "系統功能管理（改）",
            ActionType = "P",
            ParentId = "SysConfig",
            DelYn = "N",
            IsMenu = "Y",
            IsEnabled = "Y",
            SortOrder = 1,
            FunDesc = "new desc"
        };
        _repository.GetByFunIdAsync("FunctionList", Arg.Any<CancellationToken>())
            .Returns(existing, updated);
        _repository.GetByFunIdAsync("SysConfig", Arg.Any<CancellationToken>())
            .Returns(new SysFun { FunId = "SysConfig", FunName = "系統參數管理", DelYn = "N" });
        _repository.UpdateAsync(Arg.Any<SysFun>(), Arg.Any<CancellationToken>()).Returns(1);

        var result = await _sut.UpdateAsync("FunctionList", new SaveFunctionRequest
        {
            FunId = "FunctionList",
            FunName = "系統功能管理（改）",
            ActionType = "P",
            ParentId = "SysConfig",
            SortOrder = 1,
            IsMenu = "Y",
            IsEnabled = "Y",
            FunDesc = "new desc"
        });

        result.Code.ShouldBe("100");
        result.Data!.FunName.ShouldBe("系統功能管理（改）");
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenParentIsDescendant_ReturnsValidationError()
    {
        _repository.GetByFunIdAsync("ROOT", Arg.Any<CancellationToken>())
            .Returns(new SysFun { FunId = "ROOT", FunName = "根功能", ActionType = "P", DelYn = "N" });
        _repository.GetByFunIdAsync("CHILD", Arg.Any<CancellationToken>())
            .Returns(new SysFun { FunId = "CHILD", FunName = "子功能", DelYn = "N" });
        _repository.IsDescendantAsync("ROOT", "CHILD", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.UpdateAsync("ROOT", new SaveFunctionRequest
        {
            FunId = "ROOT",
            FunName = "根功能",
            ActionType = "P",
            ParentId = "CHILD",
            SortOrder = 1,
            IsMenu = "Y",
            IsEnabled = "Y"
        });

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("下層節點");
        await _repository.DidNotReceive().UpdateAsync(
            Arg.Any<SysFun>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenHasChildren_ReturnsValidationError()
    {
        _repository.GetByFunIdAsync("ROOT", Arg.Any<CancellationToken>())
            .Returns(new SysFun { FunId = "ROOT", FunName = "根功能", DelYn = "N" });
        _repository.HasActiveChildrenAsync("ROOT", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.SoftDeleteAsync("ROOT");

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("不能刪除");
        await _repository.DidNotReceive().SoftDeleteAsync(
            Arg.Any<SysFun>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenReferencedByRole_ReturnsValidationError()
    {
        _repository.GetByFunIdAsync("FunctionList", Arg.Any<CancellationToken>())
            .Returns(new SysFun { FunId = "FunctionList", FunName = "系統功能管理", DelYn = "N" });
        _repository.HasActiveChildrenAsync("FunctionList", Arg.Any<CancellationToken>()).Returns(false);
        _uow.Roles.IsFunctionReferencedAsync("FunctionList", Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.SoftDeleteAsync("FunctionList");

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("不能刪除");
        await _repository.DidNotReceive().SoftDeleteAsync(
            Arg.Any<SysFun>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CanDeleteAsync_WhenClear_ReturnsSuccess()
    {
        _repository.GetByFunIdAsync("FunctionList", Arg.Any<CancellationToken>())
            .Returns(new SysFun { FunId = "FunctionList", FunName = "系統功能管理", DelYn = "N" });
        _repository.HasActiveChildrenAsync("FunctionList", Arg.Any<CancellationToken>()).Returns(false);
        _uow.Roles.IsFunctionReferencedAsync("FunctionList", Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CanDeleteAsync("FunctionList");

        result.Code.ShouldBe("100");
        result.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenValid_CommitsChange()
    {
        var existing = new SysFun { FunId = "FunctionList", FunName = "系統功能管理", DelYn = "N" };
        _repository.GetByFunIdAsync("FunctionList", Arg.Any<CancellationToken>()).Returns(existing);
        _repository.HasActiveChildrenAsync("FunctionList", Arg.Any<CancellationToken>()).Returns(false);
        _uow.Roles.IsFunctionReferencedAsync("FunctionList", Arg.Any<CancellationToken>()).Returns(false);
        _repository.SoftDeleteAsync(Arg.Any<SysFun>(), Arg.Any<CancellationToken>()).Returns(1);

        var result = await _sut.SoftDeleteAsync("FunctionList");

        result.Code.ShouldBe("100");
        result.Data.ShouldBeTrue();
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _repository.Received(1).SoftDeleteAsync(
            Arg.Is<SysFun>(x => x.DelYn == "Y" && x.ChgPerson == "tester"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenRepositoryThrows_RollsBack()
    {
        var existing = new SysFun { FunId = "FunctionList", FunName = "系統功能管理", DelYn = "N" };
        _repository.GetByFunIdAsync("FunctionList", Arg.Any<CancellationToken>()).Returns(existing);
        _repository.HasActiveChildrenAsync("FunctionList", Arg.Any<CancellationToken>()).Returns(false);
        _uow.Roles.IsFunctionReferencedAsync("FunctionList", Arg.Any<CancellationToken>()).Returns(false);
        _repository.SoftDeleteAsync(Arg.Any<SysFun>(), Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new InvalidOperationException("database failure"));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _sut.SoftDeleteAsync("FunctionList"));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
