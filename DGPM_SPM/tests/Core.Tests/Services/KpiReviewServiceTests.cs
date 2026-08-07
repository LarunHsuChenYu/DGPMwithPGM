using System.Data;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Mapping;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Application.Services;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Tests.Services;

public class KpiReviewServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IKpiDataRepository _repository = Substitute.For<IKpiDataRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly KpiReviewService _sut;

    public KpiReviewServiceTests()
    {
        _uow.KpiDatas.Returns(_repository);
        _currentUser.UserId.Returns("tester");
        _requestContext.TraceId.Returns("test-trace");
        _sut = new KpiReviewService(_uow, new KpiReviewMapper(), _currentUser, _requestContext);
    }

    private static KpiData CreateData(long dataId = 1, string reviewStatus = "D", decimal? value = 100m)
        => new()
        {
            DataId = dataId,
            DealerId = 1,
            DealerCode = "D001",
            DealerName = "測試經銷商",
            IndicatorId = 2,
            IndicatorCode = "SALES_QTY",
            IndicatorName = "銷售台數",
            PeriodYm = "202607",
            KpiValue = value,
            ReviewStatus = reviewStatus
        };

    // ---------- GetPagedAsync ----------

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPage()
    {
        var filter = new KpiDataFilter { PeriodYm = " 202607 ", ReviewStatus = "d", Page = 2, PageSize = 10 };
        _repository.GetPagedAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<KpiData>
            {
                Datas = [CreateData()],
                TotalRow = 11,
                Page = 2,
                PageSize = 10
            });

        var result = await _sut.GetPagedAsync(filter);

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.Datas.Single().DealerCode.ShouldBe("D001");
        result.Data.Datas.Single().IndicatorName.ShouldBe("銷售台數");
        result.Data.TotalRow.ShouldBe(11);
        filter.PeriodYm.ShouldBe("202607");
        filter.ReviewStatus.ShouldBe("D");
    }

    [Fact]
    public async Task GetPagedAsync_WithInvalidPeriodYm_ReturnsInvalidParameter()
    {
        var result = await _sut.GetPagedAsync(new KpiDataFilter { PeriodYm = "2026-07" });

        result.Code.ShouldBe("200");
        await _repository.DidNotReceive().GetPagedAsync(
            Arg.Any<KpiDataFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPagedAsync_WithInvalidReviewStatus_ReturnsInvalidParameter()
    {
        var result = await _sut.GetPagedAsync(new KpiDataFilter { ReviewStatus = "X" });

        result.Code.ShouldBe("200");
        await _repository.DidNotReceive().GetPagedAsync(
            Arg.Any<KpiDataFilter>(),
            Arg.Any<CancellationToken>());
    }

    // ---------- ReviewAsync ----------

    [Fact]
    public async Task ReviewAsync_FromDraft_CommitsAndWritesChangeLog()
    {
        var draft = CreateData(dataId: 5, reviewStatus: "D");
        var locked = CreateData(dataId: 5, reviewStatus: "R");
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(draft, locked);
        _repository.UpdateReviewStatusAsync(5, "R", "tester", Arg.Any<CancellationToken>()).Returns(1);

        var result = await _sut.ReviewAsync(5, new ReviewKpiDataRequest { Memo = " 數字已核對 " });

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.ReviewStatus.ShouldBe("R");
        await _uow.Received(1).BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _repository.Received(1).AddChangeLogAsync(
            Arg.Is<KpiChangeLog>(x =>
                x.DataId == 5 &&
                x.ActionType == "R" &&
                x.Reason == "數字已核對" &&
                x.ActionUser == "tester"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReviewAsync_FromUnlocked_Succeeds()
    {
        var unlocked = CreateData(dataId: 7, reviewStatus: "U");
        var locked = CreateData(dataId: 7, reviewStatus: "R");
        _repository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(unlocked, locked);
        _repository.UpdateReviewStatusAsync(7, "R", "tester", Arg.Any<CancellationToken>()).Returns(1);

        var result = await _sut.ReviewAsync(7, new ReviewKpiDataRequest());

        result.Code.ShouldBe("100");
        await _repository.Received(1).AddChangeLogAsync(
            Arg.Is<KpiChangeLog>(x => x.ActionType == "R" && x.Reason == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReviewAsync_WhenAlreadyLocked_ReturnsInvalidParameter()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(CreateData(dataId: 5, reviewStatus: "R"));

        var result = await _sut.ReviewAsync(5, new ReviewKpiDataRequest());

        result.Code.ShouldBe("200");
        await _uow.DidNotReceive().BeginTransactionAsync(
            Arg.Any<IsolationLevel>(),
            Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().UpdateReviewStatusAsync(
            Arg.Any<long>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReviewAsync_WhenNotFound_ReturnsDataNotFound()
    {
        _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((KpiData?)null);

        var result = await _sut.ReviewAsync(99, new ReviewKpiDataRequest());

        result.Code.ShouldBe("404");
        await _uow.DidNotReceive().BeginTransactionAsync(
            Arg.Any<IsolationLevel>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReviewAsync_WhenRepositoryThrows_RollsBack()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(CreateData(dataId: 5, reviewStatus: "D"));
        _repository.UpdateReviewStatusAsync(5, "R", "tester", Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new InvalidOperationException("database failure"));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _sut.ReviewAsync(5, new ReviewKpiDataRequest()));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    // ---------- UnlockAsync ----------

    [Fact]
    public async Task UnlockAsync_FromLocked_CommitsAndWritesChangeLog()
    {
        var locked = CreateData(dataId: 5, reviewStatus: "R");
        var unlocked = CreateData(dataId: 5, reviewStatus: "U");
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(locked, unlocked);
        _repository.UpdateReviewStatusAsync(5, "U", "tester", Arg.Any<CancellationToken>()).Returns(1);

        var result = await _sut.UnlockAsync(5, new UnlockKpiDataRequest { Reason = " 數值有誤需修正 " });

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.ReviewStatus.ShouldBe("U");
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _repository.Received(1).AddChangeLogAsync(
            Arg.Is<KpiChangeLog>(x =>
                x.DataId == 5 &&
                x.ActionType == "U" &&
                x.Reason == "數值有誤需修正" &&
                x.ActionUser == "tester"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnlockAsync_WithoutReason_ReturnsInvalidParameter()
    {
        var result = await _sut.UnlockAsync(5, new UnlockKpiDataRequest { Reason = "  " });

        result.Code.ShouldBe("200");
        await _repository.DidNotReceive().GetByIdAsync(
            Arg.Any<long>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnlockAsync_WhenNotLocked_ReturnsInvalidParameter()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(CreateData(dataId: 5, reviewStatus: "D"));

        var result = await _sut.UnlockAsync(5, new UnlockKpiDataRequest { Reason = "退回修正" });

        result.Code.ShouldBe("200");
        await _uow.DidNotReceive().BeginTransactionAsync(
            Arg.Any<IsolationLevel>(),
            Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().UpdateReviewStatusAsync(
            Arg.Any<long>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnlockAsync_WithoutOperator_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns(string.Empty);

        var result = await _sut.UnlockAsync(5, new UnlockKpiDataRequest { Reason = "退回修正" });

        result.Code.ShouldBe("400");
        await _repository.DidNotReceive().GetByIdAsync(
            Arg.Any<long>(),
            Arg.Any<CancellationToken>());
    }
}
