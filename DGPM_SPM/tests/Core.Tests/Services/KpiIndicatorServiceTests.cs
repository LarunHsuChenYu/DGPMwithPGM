using System.Data;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Mapping;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Kpi;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Application.Services;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Tests.Services;

public class KpiIndicatorServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IKpiIndicatorRepository _repository = Substitute.For<IKpiIndicatorRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly KpiIndicatorService _sut;

    public KpiIndicatorServiceTests()
    {
        _uow.KpiIndicators.Returns(_repository);
        _currentUser.UserId.Returns("tester");
        _requestContext.TraceId.Returns("test-trace");
        _sut = new KpiIndicatorService(_uow, new KpiIndicatorMapper(), _currentUser, _requestContext);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPage()
    {
        var filter = new KpiIndicatorFilter { Keyword = " 銷售 ", Status = "a", Page = 2, PageSize = 10 };
        _repository.GetPagedAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<KpiIndicator>
            {
                Datas =
                [
                    new()
                    {
                        IndicatorId = 1,
                        IndicatorCode = "SALES_QTY",
                        IndicatorName = "銷售台數",
                        DataType = "N",
                        DecimalPlaces = 0,
                        Status = "A"
                    }
                ],
                TotalRow = 11,
                Page = 2,
                PageSize = 10
            });

        var result = await _sut.GetPagedAsync(filter);

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.Datas.Single().IndicatorCode.ShouldBe("SALES_QTY");
        result.Data.TotalRow.ShouldBe(11);
        filter.Keyword.ShouldBe("銷售");
        filter.Status.ShouldBe("A");
    }

    [Fact]
    public async Task GetPagedAsync_WithInvalidStatus_ReturnsInvalidParameter()
    {
        var result = await _sut.GetPagedAsync(new KpiIndicatorFilter { Status = "X" });

        result.Code.ShouldBe("200");
        await _repository.DidNotReceive().GetPagedAsync(
            Arg.Any<KpiIndicatorFilter>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CommitsTransaction()
    {
        var request = new SaveKpiIndicatorRequest
        {
            IndicatorCode = " sales_qty ",
            IndicatorName = " 銷售台數 ",
            Unit = " 台 ",
            DataType = "n",
            DecimalPlaces = 0,
            SortOrder = 1,
            Memo = " 每月銷售台數 "
        };
        _repository.ExistsByCodeAsync("SALES_QTY", null, Arg.Any<CancellationToken>())
            .Returns(false);
        _repository.AddAsync(Arg.Any<KpiIndicator>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var entity = call.Arg<KpiIndicator>();
                entity.IndicatorId = 10;
                return entity;
            });

        var result = await _sut.CreateAsync(request);

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.IndicatorId.ShouldBe(10);
        result.Data.Status.ShouldBe("A");
        await _uow.Received(1).BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAsync(
            Arg.Is<KpiIndicator>(x =>
                x.IndicatorCode == "SALES_QTY" &&
                x.IndicatorName == "銷售台數" &&
                x.Unit == "台" &&
                x.DataType == "N" &&
                x.CrtUser == "tester" &&
                x.Memo == "每月銷售台數"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidDataType_DoesNotWrite()
    {
        var result = await _sut.CreateAsync(new SaveKpiIndicatorRequest
        {
            IndicatorCode = "SALES_QTY",
            IndicatorName = "銷售台數",
            DataType = "X"
        });

        result.Code.ShouldBe("200");
        await _repository.DidNotReceive().AddAsync(
            Arg.Any<KpiIndicator>(),
            Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().BeginTransactionAsync(
            Arg.Any<IsolationLevel>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCode_ReturnsDuplicate()
    {
        _repository.ExistsByCodeAsync("SALES_QTY", null, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.CreateAsync(new SaveKpiIndicatorRequest
        {
            IndicatorCode = "SALES_QTY",
            IndicatorName = "銷售台數",
            DataType = "N"
        });

        result.Code.ShouldBe("409");
        await _repository.DidNotReceive().AddAsync(
            Arg.Any<KpiIndicator>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsDataNotFound()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns((KpiIndicator?)null);

        var result = await _sut.UpdateAsync(5, new SaveKpiIndicatorRequest
        {
            IndicatorCode = "SALES_QTY",
            IndicatorName = "銷售台數",
            DataType = "N"
        });

        result.Code.ShouldBe("404");
        await _repository.DidNotReceive().UpdateAsync(
            Arg.Any<KpiIndicator>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateCode_DoesNotWrite()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(new KpiIndicator { IndicatorId = 5, IndicatorCode = "OLD_CODE", IndicatorName = "舊指標" });
        _repository.ExistsByCodeAsync("SALES_QTY", 5, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.UpdateAsync(5, new SaveKpiIndicatorRequest
        {
            IndicatorCode = "SALES_QTY",
            IndicatorName = "銷售台數",
            DataType = "N"
        });

        result.Code.ShouldBe("409");
        await _repository.DidNotReceive().UpdateAsync(
            Arg.Any<KpiIndicator>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_CommitsAndReturnsUpdated()
    {
        var existing = new KpiIndicator
        {
            IndicatorId = 5,
            IndicatorCode = "OLD_CODE",
            IndicatorName = "舊指標",
            DataType = "N",
            Status = "A"
        };
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(existing);
        _repository.ExistsByCodeAsync("SALES_QTY", 5, Arg.Any<CancellationToken>()).Returns(false);
        _repository.UpdateAsync(existing, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.UpdateAsync(5, new SaveKpiIndicatorRequest
        {
            IndicatorCode = "SALES_QTY",
            IndicatorName = "銷售台數",
            DataType = "P",
            DecimalPlaces = 2
        });

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.IndicatorCode.ShouldBe("SALES_QTY");
        result.Data.DataType.ShouldBe("P");
        existing.MdfUser.ShouldBe("tester");
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetStatusAsync_WhenRepositoryThrows_RollsBack()
    {
        _repository.SetStatusAsync(5, "I", "tester", Arg.Any<CancellationToken>())
            .Returns<Task<KpiIndicator?>>(_ => throw new InvalidOperationException("database failure"));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _sut.SetStatusAsync(5, new SetKpiIndicatorStatusRequest { Status = "I" }));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetStatusAsync_WithInvalidStatus_ReturnsInvalidParameter()
    {
        var result = await _sut.SetStatusAsync(5, new SetKpiIndicatorStatusRequest { Status = "X" });

        result.Code.ShouldBe("200");
        await _repository.DidNotReceive().SetStatusAsync(
            Arg.Any<int>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
