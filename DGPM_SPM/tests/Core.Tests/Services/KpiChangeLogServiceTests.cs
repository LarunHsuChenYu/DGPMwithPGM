using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Mapping;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Application.Services;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Tests.Services;

public class KpiChangeLogServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IKpiChangeLogRepository _repository = Substitute.For<IKpiChangeLogRepository>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly KpiChangeLogService _sut;

    public KpiChangeLogServiceTests()
    {
        _uow.KpiChangeLogs.Returns(_repository);
        _requestContext.TraceId.Returns("test-trace");
        _sut = new KpiChangeLogService(_uow, new KpiChangeLogMapper(), _requestContext);
    }

    private static KpiChangeLog CreateLog(long logId = 1, string actionType = "I")
        => new()
        {
            LogId = logId,
            DataId = 10,
            ActionType = actionType,
            OldValue = null,
            NewValue = 100m,
            Reason = "初次匯入",
            ActionUser = "importer",
            ActionDate = new DateTime(2026, 7, 15, 10, 30, 0),
            PeriodYm = "202607",
            DealerCode = "D001",
            DealerName = "測試經銷商",
            IndicatorCode = "SALES_QTY",
            IndicatorName = "銷售台數",
            Unit = "台"
        };

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPage_AndNormalizesFilter()
    {
        var filter = new KpiChangeLogFilter
        {
            PeriodYm = " 202607 ",
            Keyword = " 銷售 ",
            ActionType = "i",
            ActionUser = " importer ",
            Page = 2,
            PageSize = 10
        };
        _repository.GetPagedAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<KpiChangeLog>
            {
                Datas = [CreateLog()],
                TotalRow = 11,
                Page = 2,
                PageSize = 10
            });

        var result = await _sut.GetPagedAsync(filter);

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        var dto = result.Data.Datas.Single();
        dto.LogId.ShouldBe(1);
        dto.DealerCode.ShouldBe("D001");
        dto.IndicatorName.ShouldBe("銷售台數");
        dto.ActionType.ShouldBe("I");
        dto.NewValue.ShouldBe(100m);
        result.Data.TotalRow.ShouldBe(11);
        filter.PeriodYm.ShouldBe("202607");
        filter.Keyword.ShouldBe("銷售");
        filter.ActionType.ShouldBe("I");
        filter.ActionUser.ShouldBe("importer");
    }

    [Fact]
    public async Task GetPagedAsync_BlankFilterValues_NormalizedToNull()
    {
        var filter = new KpiChangeLogFilter
        {
            PeriodYm = "  ",
            Keyword = "",
            ActionType = " ",
            ActionUser = "  "
        };
        _repository.GetPagedAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<KpiChangeLog> { Datas = [], TotalRow = 0, Page = 1, PageSize = 20 });

        var result = await _sut.GetPagedAsync(filter);

        result.Code.ShouldBe("100");
        filter.PeriodYm.ShouldBeNull();
        filter.Keyword.ShouldBeNull();
        filter.ActionType.ShouldBeNull();
        filter.ActionUser.ShouldBeNull();
    }

    [Fact]
    public async Task GetPagedAsync_WithInvalidPeriodYm_ReturnsInvalidParameter()
    {
        var result = await _sut.GetPagedAsync(new KpiChangeLogFilter { PeriodYm = "2026-07" });

        result.Code.ShouldBe("200");
        await _repository.DidNotReceive().GetPagedAsync(
            Arg.Any<KpiChangeLogFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPagedAsync_WithInvalidActionType_ReturnsInvalidParameter()
    {
        var result = await _sut.GetPagedAsync(new KpiChangeLogFilter { ActionType = "X" });

        result.Code.ShouldBe("200");
        await _repository.DidNotReceive().GetPagedAsync(
            Arg.Any<KpiChangeLogFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPagedAsync_WithReversedDateRange_ReturnsInvalidParameter()
    {
        var result = await _sut.GetPagedAsync(new KpiChangeLogFilter
        {
            ActionDateFrom = new DateTime(2026, 7, 20),
            ActionDateTo = new DateTime(2026, 7, 10)
        });

        result.Code.ShouldBe("200");
        await _repository.DidNotReceive().GetPagedAsync(
            Arg.Any<KpiChangeLogFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPagedAsync_WithSameDayDateRange_Succeeds()
    {
        var filter = new KpiChangeLogFilter
        {
            ActionDateFrom = new DateTime(2026, 7, 15),
            ActionDateTo = new DateTime(2026, 7, 15)
        };
        _repository.GetPagedAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<KpiChangeLog> { Datas = [], TotalRow = 0, Page = 1, PageSize = 20 });

        var result = await _sut.GetPagedAsync(filter);

        result.Code.ShouldBe("100");
        await _repository.Received(1).GetPagedAsync(filter, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPagedAsync_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var filter = new KpiChangeLogFilter();
        _repository.GetPagedAsync(filter, cts.Token)
            .Returns(new PagedResult<KpiChangeLog> { Datas = [], TotalRow = 0, Page = 1, PageSize = 20 });

        var result = await _sut.GetPagedAsync(filter, cts.Token);

        result.Code.ShouldBe("100");
        await _repository.Received(1).GetPagedAsync(filter, cts.Token);
    }
}
