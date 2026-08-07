using System.Data;
using Microsoft.Extensions.Caching.Memory;
using PGM.Core.Application.Interfaces;
using PGM.Core.Application.Mapping;
using PGM.Core.Application.Models.Parameter;
using PGM.Core.Application.Services;
using PGM.Core.Domain.Entities;

namespace PGM.Core.Tests.Services;

public class ParameterServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IParameterRepository _paramRepo = Substitute.For<IParameterRepository>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly IParameterMapper _mapper = new ParameterMapper();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ParameterService _sut;

    public ParameterServiceTests()
    {
        _uow.Parameters.Returns(_paramRepo);
        _requestContext.TraceId.Returns("test-trace");
        _currentUser.UserId.Returns("tester");
        _sut = new ParameterService(_uow, _cache, _mapper, _requestContext, _currentUser);
    }

    [Fact]
    public async Task GetParameterListAsync_ReturnsMappedItems()
    {
        _paramRepo.GetAllByItemAsync("STATUS", Arg.Any<CancellationToken>())
            .Returns(new List<Parameter>
            {
                new() { SetItem = "STATUS", SetId = "A", SetValue = "Active", SortOrder = 1 },
                new() { SetItem = "STATUS", SetId = "I", SetValue = "Inactive", SortOrder = 2 }
            });

        var result = await _sut.GetParameterListAsync("STATUS");

        result.Data.ShouldNotBeNull();
        result.Data.Count.ShouldBe(2);
        result.Data[0].SetId.ShouldBe("A");
        result.Data[0].SetValue.ShouldBe("Active");
    }

    [Fact]
    public async Task GetParameterListAsync_UsesCacheOnSecondCall()
    {
        _paramRepo.GetAllByItemAsync("TYPE", Arg.Any<CancellationToken>())
            .Returns(new List<Parameter>
            {
                new() { SetItem = "TYPE", SetId = "X", SetValue = "One", SortOrder = 1 }
            });

        await _sut.GetParameterListAsync("TYPE");
        await _sut.GetParameterListAsync("TYPE");

        await _paramRepo.Received(1).GetAllByItemAsync("TYPE", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetParameterListAsync_WithEmptySetItem_ReturnsEmpty()
    {
        var result = await _sut.GetParameterListAsync("");

        result.Data.ShouldNotBeNull();
        result.Data.ShouldBeEmpty();
        await _paramRepo.DidNotReceive().GetAllByItemAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsActiveCategories()
    {
        _paramRepo.GetActiveCategoriesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ParamItem>
            {
                new() { SetItem = "SAMPLE_STATUS", SetItemName = "狀態範例" }
            });

        var result = await _sut.GetCategoriesAsync();

        result.Data.ShouldNotBeNull();
        result.Data.Count.ShouldBe(1);
        result.Data[0].SetItem.ShouldBe("SAMPLE_STATUS");
    }

    [Fact]
    public async Task CreateAsync_WhenActiveDuplicate_ReturnsError()
    {
        _paramRepo.IsCategoryActiveAsync("SAMPLE_STATUS", Arg.Any<CancellationToken>()).Returns(true);
        _paramRepo.GetByKeyAsync("SAMPLE_STATUS", "A", Arg.Any<CancellationToken>())
            .Returns(new Parameter { SetItem = "SAMPLE_STATUS", SetId = "A", DelFlg = false });

        var result = await _sut.CreateAsync(new CreateParameterRequest
        {
            SetItem = "SAMPLE_STATUS",
            SetId = "A",
            SetValue = "啟用",
            SortOrder = 1
        });

        result.Code.ShouldNotBe("100");
        result.Message.ShouldBe("此代碼已存在");
        await _paramRepo.DidNotReceive().AddAsync(Arg.Any<Parameter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenDeletedExists_Revives()
    {
        _paramRepo.IsCategoryActiveAsync("SAMPLE_STATUS", Arg.Any<CancellationToken>()).Returns(true);
        _paramRepo.GetByKeyAsync("SAMPLE_STATUS", "A", Arg.Any<CancellationToken>())
            .Returns(
                new Parameter { SetItem = "SAMPLE_STATUS", SetId = "A", DelFlg = true, SetValue = "舊" },
                new Parameter
                {
                    SetItem = "SAMPLE_STATUS",
                    SetId = "A",
                    DelFlg = false,
                    SetValue = "啟用",
                    SortOrder = 3
                });
        _paramRepo.GetCategoryNameAsync("SAMPLE_STATUS", Arg.Any<CancellationToken>()).Returns("狀態範例");
        _uow.BeginTransactionAsync(Arg.Any<IsolationLevel>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(new CreateParameterRequest
        {
            SetItem = "SAMPLE_STATUS",
            SetId = "A",
            SetValue = "啟用",
            SortOrder = 3
        });

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data!.SetValue.ShouldBe("啟用");
        await _paramRepo.Received(1).ReviveAsync(Arg.Any<Parameter>(), Arg.Any<CancellationToken>());
        await _paramRepo.DidNotReceive().AddAsync(Arg.Any<Parameter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenNew_InsertsAndInvalidatesCache()
    {
        _paramRepo.GetAllByItemAsync("SAMPLE_STATUS", Arg.Any<CancellationToken>())
            .Returns(new List<Parameter>
            {
                new() { SetItem = "SAMPLE_STATUS", SetId = "Z", SetValue = "cached", SortOrder = 1 }
            });
        await _sut.GetParameterListAsync("SAMPLE_STATUS");

        _paramRepo.IsCategoryActiveAsync("SAMPLE_STATUS", Arg.Any<CancellationToken>()).Returns(true);
        _paramRepo.GetByKeyAsync("SAMPLE_STATUS", "B", Arg.Any<CancellationToken>())
            .Returns(
                (Parameter?)null,
                new Parameter
                {
                    SetItem = "SAMPLE_STATUS",
                    SetId = "B",
                    SetValue = "新代碼",
                    SortOrder = 2,
                    DelFlg = false
                });
        _paramRepo.GetCategoryNameAsync("SAMPLE_STATUS", Arg.Any<CancellationToken>()).Returns("狀態範例");
        _uow.BeginTransactionAsync(Arg.Any<IsolationLevel>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var createResult = await _sut.CreateAsync(new CreateParameterRequest
        {
            SetItem = "SAMPLE_STATUS",
            SetId = "B",
            SetValue = "新代碼",
            SortOrder = 2
        });

        createResult.Code.ShouldBe("100");
        await _paramRepo.Received(1).AddAsync(Arg.Any<Parameter>(), Arg.Any<CancellationToken>());

        _paramRepo.GetAllByItemAsync("SAMPLE_STATUS", Arg.Any<CancellationToken>())
            .Returns(new List<Parameter>
            {
                new() { SetItem = "SAMPLE_STATUS", SetId = "B", SetValue = "新代碼", SortOrder = 2 }
            });
        await _sut.GetParameterListAsync("SAMPLE_STATUS");
        await _paramRepo.Received(2).GetAllByItemAsync("SAMPLE_STATUS", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_UpdatesValueAndSort()
    {
        _paramRepo.GetByKeyAsync("SAMPLE_STATUS", "A", Arg.Any<CancellationToken>())
            .Returns(
                new Parameter
                {
                    SetItem = "SAMPLE_STATUS",
                    SetId = "A",
                    SetValue = "舊",
                    SortOrder = 1,
                    DelFlg = false
                },
                new Parameter
                {
                    SetItem = "SAMPLE_STATUS",
                    SetId = "A",
                    SetValue = "新",
                    SortOrder = 5,
                    DelFlg = false
                });
        _paramRepo.GetCategoryNameAsync("SAMPLE_STATUS", Arg.Any<CancellationToken>()).Returns("狀態範例");
        _uow.BeginTransactionAsync(Arg.Any<IsolationLevel>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await _sut.UpdateAsync(
            "SAMPLE_STATUS",
            "A",
            new UpdateParameterRequest { SetValue = "新", SortOrder = 5 });

        result.Code.ShouldBe("100");
        result.Data!.SetValue.ShouldBe("新");
        result.Data.SortOrder.ShouldBe(5);
        await _paramRepo.Received(1).UpdateAsync(Arg.Any<Parameter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes()
    {
        _paramRepo.GetByKeyAsync("SAMPLE_STATUS", "A", Arg.Any<CancellationToken>())
            .Returns(new Parameter
            {
                SetItem = "SAMPLE_STATUS",
                SetId = "A",
                DelFlg = false
            });
        _uow.BeginTransactionAsync(Arg.Any<IsolationLevel>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _uow.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await _sut.DeleteAsync("SAMPLE_STATUS", "A");

        result.Code.ShouldBe("100");
        result.Data.ShouldBeTrue();
        await _paramRepo.Received(1).SoftDeleteAsync(Arg.Any<Parameter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetNextSortOrderAsync_ReturnsRepoValue()
    {
        _paramRepo.IsCategoryActiveAsync("SAMPLE_STATUS", Arg.Any<CancellationToken>()).Returns(true);
        _paramRepo.GetNextSortOrderAsync("SAMPLE_STATUS", Arg.Any<CancellationToken>()).Returns(4);

        var result = await _sut.GetNextSortOrderAsync("SAMPLE_STATUS");

        result.Code.ShouldBe("100");
        result.Data.ShouldBe(4);
    }
}
