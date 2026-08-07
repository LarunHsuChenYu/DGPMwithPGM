using System.Data;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Mapping;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Application.Services;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Tests.Services;

public class KpiImportServiceTests
{
    private const long BatchId = 77;
    private const string PeriodYm = "202607";

    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IKpiImportRepository _importRepo = Substitute.For<IKpiImportRepository>();
    private readonly IDealerRepository _dealerRepo = Substitute.For<IDealerRepository>();
    private readonly IKpiIndicatorRepository _indicatorRepo = Substitute.For<IKpiIndicatorRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly KpiImportService _sut;

    public KpiImportServiceTests()
    {
        _uow.KpiImports.Returns(_importRepo);
        _uow.Dealers.Returns(_dealerRepo);
        _uow.KpiIndicators.Returns(_indicatorRepo);
        _currentUser.UserId.Returns("tester");
        _requestContext.TraceId.Returns("test-trace");
        _sut = new KpiImportService(_uow, new KpiImportMapper(), _currentUser, _requestContext);
    }

    private void SetupMasterData(
        IReadOnlyList<Dealer>? dealers = null,
        IReadOnlyList<KpiIndicator>? indicators = null,
        IReadOnlyList<KpiData>? existingData = null)
    {
        _importRepo.AddBatchAsync(Arg.Any<KpiImportBatch>(), Arg.Any<CancellationToken>())
            .Returns(BatchId);
        _dealerRepo.GetActiveByCodesAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(dealers ?? [new Dealer { DealerId = 1, DealerCode = "D001", DealerName = "測試經銷商" }]);
        _indicatorRepo.GetActiveByCodesAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(indicators ?? [new KpiIndicator { IndicatorId = 10, IndicatorCode = "SALES_QTY", IndicatorName = "銷售台數" }]);
        _importRepo.GetDataByPeriodAsync(PeriodYm, Arg.Any<CancellationToken>())
            .Returns(existingData ?? []);
        _importRepo.AddDataAsync(Arg.Any<KpiData>(), Arg.Any<CancellationToken>())
            .Returns(100L);
        _importRepo.GetBatchByIdAsync(BatchId, Arg.Any<CancellationToken>())
            .Returns(call => new KpiImportBatch { BatchId = BatchId, PeriodYm = PeriodYm, ImportUser = "tester" });
    }

    private static CreateKpiImportRequest BuildRequest(params KpiImportRowRequest[] rows)
        => new()
        {
            PeriodYm = PeriodYm,
            FileName = " 手動輸入 ",
            Rows = rows.ToList()
        };

    [Fact]
    public async Task ImportAsync_WithNewRow_AddsDataAndChangeLog_AndCommits()
    {
        SetupMasterData();
        var request = BuildRequest(new KpiImportRowRequest
        {
            DealerCode = " d001 ",
            IndicatorCode = " sales_qty ",
            Value = " 120.5 "
        });

        var result = await _sut.ImportAsync(request);

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.Batch.BatchId.ShouldBe(BatchId);
        result.Data.RowResults.Single().Success.ShouldBeTrue();

        await _uow.Received(1).BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());

        await _importRepo.Received(1).AddDataAsync(
            Arg.Is<KpiData>(x =>
                x.DealerId == 1 &&
                x.IndicatorId == 10 &&
                x.PeriodYm == PeriodYm &&
                x.KpiValue == 120.5m &&
                x.BatchId == BatchId &&
                x.ReviewStatus == "D" &&
                x.CrtUser == "tester"),
            Arg.Any<CancellationToken>());
        await _importRepo.Received(1).AddChangeLogAsync(
            Arg.Is<KpiChangeLog>(x =>
                x.DataId == 100L &&
                x.ActionType == "I" &&
                x.OldValue == null &&
                x.NewValue == 120.5m &&
                x.ActionUser == "tester"),
            Arg.Any<CancellationToken>());
        await _importRepo.Received(1).UpdateBatchResultAsync(
            Arg.Is<KpiImportBatch>(x =>
                x.BatchId == BatchId &&
                x.ImportStatus == "S" &&
                x.TotalRows == 1 &&
                x.SuccessRows == 1 &&
                x.FailRows == 0 &&
                x.ErrorMessage == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportAsync_WithExistingDraftData_UpdatesValueAndLogsOldValue()
    {
        var existing = new KpiData
        {
            DataId = 55,
            DealerId = 1,
            IndicatorId = 10,
            PeriodYm = PeriodYm,
            KpiValue = 99m,
            ReviewStatus = "D"
        };
        SetupMasterData(existingData: [existing]);
        var request = BuildRequest(new KpiImportRowRequest
        {
            DealerCode = "D001",
            IndicatorCode = "SALES_QTY",
            Value = "120"
        });

        var result = await _sut.ImportAsync(request);

        result.Code.ShouldBe("100");
        result.Data!.RowResults.Single().Success.ShouldBeTrue();

        await _importRepo.DidNotReceive().AddDataAsync(Arg.Any<KpiData>(), Arg.Any<CancellationToken>());
        await _importRepo.Received(1).UpdateDataValueAsync(
            Arg.Is<KpiData>(x =>
                x.DataId == 55 &&
                x.KpiValue == 120m &&
                x.BatchId == BatchId &&
                x.ReviewStatus == "D" &&
                x.MdfUser == "tester"),
            Arg.Any<CancellationToken>());
        await _importRepo.Received(1).AddChangeLogAsync(
            Arg.Is<KpiChangeLog>(x =>
                x.DataId == 55 &&
                x.ActionType == "I" &&
                x.OldValue == 99m &&
                x.NewValue == 120m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportAsync_WithLockedData_MarksRowFailed_AndDoesNotWriteData()
    {
        var locked = new KpiData
        {
            DataId = 55,
            DealerId = 1,
            IndicatorId = 10,
            PeriodYm = PeriodYm,
            KpiValue = 99m,
            ReviewStatus = "R"
        };
        SetupMasterData(existingData: [locked]);
        var request = BuildRequest(new KpiImportRowRequest
        {
            DealerCode = "D001",
            IndicatorCode = "SALES_QTY",
            Value = "120"
        });

        var result = await _sut.ImportAsync(request);

        result.Code.ShouldBe("100");
        var row = result.Data!.RowResults.Single();
        row.Success.ShouldBeFalse();
        row.ErrorMessage.ShouldNotBeNullOrWhiteSpace();

        await _importRepo.DidNotReceive().AddDataAsync(Arg.Any<KpiData>(), Arg.Any<CancellationToken>());
        await _importRepo.DidNotReceive().UpdateDataValueAsync(Arg.Any<KpiData>(), Arg.Any<CancellationToken>());
        await _importRepo.Received(1).UpdateBatchResultAsync(
            Arg.Is<KpiImportBatch>(x => x.ImportStatus == "F" && x.FailRows == 1 && x.ErrorMessage != null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportAsync_WithUnknownDealerAndBadValue_MarksRowsFailed_ButImportsValidRow()
    {
        SetupMasterData();
        var request = BuildRequest(
            new KpiImportRowRequest { DealerCode = "D001", IndicatorCode = "SALES_QTY", Value = "120" },
            new KpiImportRowRequest { DealerCode = "D999", IndicatorCode = "SALES_QTY", Value = "1" },
            new KpiImportRowRequest { DealerCode = "D001", IndicatorCode = "SALES_QTY", Value = "abc" });

        var result = await _sut.ImportAsync(request);

        result.Code.ShouldBe("100");
        result.Data!.RowResults.Count.ShouldBe(3);
        result.Data.RowResults[0].Success.ShouldBeTrue();
        result.Data.RowResults[1].Success.ShouldBeFalse();
        result.Data.RowResults[2].Success.ShouldBeFalse();

        await _importRepo.Received(1).AddDataAsync(Arg.Any<KpiData>(), Arg.Any<CancellationToken>());
        await _importRepo.Received(1).UpdateBatchResultAsync(
            Arg.Is<KpiImportBatch>(x =>
                x.ImportStatus == "F" &&
                x.TotalRows == 3 &&
                x.SuccessRows == 1 &&
                x.FailRows == 2),
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportAsync_WithDuplicateRowInBatch_MarksSecondRowFailed()
    {
        SetupMasterData();
        var request = BuildRequest(
            new KpiImportRowRequest { DealerCode = "D001", IndicatorCode = "SALES_QTY", Value = "120" },
            new KpiImportRowRequest { DealerCode = "d001", IndicatorCode = "sales_qty", Value = "130" });

        var result = await _sut.ImportAsync(request);

        result.Data!.RowResults[0].Success.ShouldBeTrue();
        result.Data.RowResults[1].Success.ShouldBeFalse();
        await _importRepo.Received(1).AddDataAsync(Arg.Any<KpiData>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026")]
    [InlineData("202613")]
    [InlineData("abcdef")]
    public async Task ImportAsync_WithInvalidPeriodYm_ReturnsInvalidParameter(string periodYm)
    {
        var result = await _sut.ImportAsync(new CreateKpiImportRequest
        {
            PeriodYm = periodYm,
            Rows = [new KpiImportRowRequest { DealerCode = "D001", IndicatorCode = "SALES_QTY", Value = "1" }]
        });

        result.Code.ShouldBe("200");
        await _uow.DidNotReceive().BeginTransactionAsync(
            Arg.Any<IsolationLevel>(),
            Arg.Any<CancellationToken>());
        await _importRepo.DidNotReceive().AddBatchAsync(
            Arg.Any<KpiImportBatch>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportAsync_WithEmptyRows_ReturnsInvalidParameter()
    {
        var result = await _sut.ImportAsync(new CreateKpiImportRequest { PeriodYm = PeriodYm });

        result.Code.ShouldBe("200");
        await _uow.DidNotReceive().BeginTransactionAsync(
            Arg.Any<IsolationLevel>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportAsync_WithoutOperator_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns((string?)null);

        var result = await _sut.ImportAsync(BuildRequest(
            new KpiImportRowRequest { DealerCode = "D001", IndicatorCode = "SALES_QTY", Value = "1" }));

        result.Code.ShouldBe("400");
        await _importRepo.DidNotReceive().AddBatchAsync(
            Arg.Any<KpiImportBatch>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportAsync_WhenRepositoryThrows_RollsBack()
    {
        SetupMasterData();
        _importRepo.AddDataAsync(Arg.Any<KpiData>(), Arg.Any<CancellationToken>())
            .Returns<Task<long>>(_ => throw new InvalidOperationException("database failure"));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _sut.ImportAsync(BuildRequest(
                new KpiImportRowRequest { DealerCode = "D001", IndicatorCode = "SALES_QTY", Value = "1" })));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBatchAsync_WhenNotFound_ReturnsDataNotFound()
    {
        _importRepo.GetBatchByIdAsync(999, Arg.Any<CancellationToken>())
            .Returns((KpiImportBatch?)null);

        var result = await _sut.GetBatchAsync(999);

        result.Code.ShouldBe("404");
    }

    [Fact]
    public async Task GetBatchAsync_WhenFound_ReturnsDto()
    {
        _importRepo.GetBatchByIdAsync(BatchId, Arg.Any<CancellationToken>())
            .Returns(new KpiImportBatch
            {
                BatchId = BatchId,
                PeriodYm = PeriodYm,
                ImportStatus = "S",
                TotalRows = 3,
                SuccessRows = 3,
                ImportUser = "tester"
            });

        var result = await _sut.GetBatchAsync(BatchId);

        result.Code.ShouldBe("100");
        result.Data!.BatchId.ShouldBe(BatchId);
        result.Data.ImportStatus.ShouldBe("S");
    }

    [Fact]
    public async Task GetBatchPagedAsync_ReturnsMappedPage()
    {
        var filter = new KpiImportBatchFilter { PeriodYm = " 202607 ", ImportStatus = " s ", Page = 1, PageSize = 10 };
        _importRepo.GetBatchPagedAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<KpiImportBatch>
            {
                Datas = [new KpiImportBatch { BatchId = 1, PeriodYm = PeriodYm, ImportStatus = "S", ImportUser = "tester" }],
                TotalRow = 1,
                Page = 1,
                PageSize = 10
            });

        var result = await _sut.GetBatchPagedAsync(filter);

        result.Code.ShouldBe("100");
        result.Data!.Datas.Single().BatchId.ShouldBe(1);
        filter.PeriodYm.ShouldBe("202607");
        filter.ImportStatus.ShouldBe("S");
    }

    [Fact]
    public async Task GetBatchPagedAsync_WithInvalidStatus_ReturnsInvalidParameter()
    {
        var result = await _sut.GetBatchPagedAsync(new KpiImportBatchFilter { ImportStatus = "X" });

        result.Code.ShouldBe("200");
        await _importRepo.DidNotReceive().GetBatchPagedAsync(
            Arg.Any<KpiImportBatchFilter>(),
            Arg.Any<CancellationToken>());
    }
}
