using System.Data;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Mapping;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.ExchangeRate;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Application.Services;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Tests.Services;

public class ExchangeRateServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IExchangeRateRepository _repository = Substitute.For<IExchangeRateRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly ExchangeRateService _sut;

    public ExchangeRateServiceTests()
    {
        _uow.ExchangeRates.Returns(_repository);
        _currentUser.UserId.Returns("tester");
        _requestContext.TraceId.Returns("test-trace");
        _sut = new ExchangeRateService(_uow, new ExchangeRateMapper(), _currentUser, _requestContext);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPage()
    {
        var filter = new ExchangeRateFilter { CurrencyCode = "usd", Page = 2, PageSize = 10 };
        _repository.GetPagedAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ExchangeRate>
            {
                Datas =
                [
                    new()
                    {
                        RateId = 1,
                        CurrencyCode = "USD",
                        RateYm = "202607",
                        RateValue = 32.5m,
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
        result.Data.Datas.Single().CurrencyCode.ShouldBe("USD");
        result.Data.TotalRow.ShouldBe(11);
        filter.CurrencyCode.ShouldBe("USD");
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CommitsTransaction()
    {
        var request = new SaveExchangeRateRequest
        {
            CurrencyCode = "usd",
            RateYm = "202607",
            RateValue = 32.5m,
            Memo = " 月匯率 "
        };
        _repository.ExistsAsync("USD", "202607", null, Arg.Any<CancellationToken>())
            .Returns(false);
        _repository.AddAsync(Arg.Any<ExchangeRate>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var entity = call.Arg<ExchangeRate>();
                entity.RateId = 10;
                return entity;
            });

        var result = await _sut.CreateAsync(request);

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.RateId.ShouldBe(10);
        result.Data.Status.ShouldBe("A");
        await _uow.Received(1).BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAsync(
            Arg.Is<ExchangeRate>(x => x.CrtUser == "tester" && x.Memo == "月匯率"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRate_DoesNotWrite()
    {
        var result = await _sut.CreateAsync(new SaveExchangeRateRequest
        {
            CurrencyCode = "USD",
            RateYm = "202613",
            RateValue = 0
        });

        result.Code.ShouldBe("200");
        await _repository.DidNotReceive().AddAsync(
            Arg.Any<ExchangeRate>(),
            Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().BeginTransactionAsync(
            Arg.Any<IsolationLevel>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateCurrencyAndMonth_DoesNotWrite()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(new ExchangeRate { RateId = 5, CurrencyCode = "JPY", RateYm = "202607" });
        _repository.ExistsAsync("USD", "202607", 5, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.UpdateAsync(5, new SaveExchangeRateRequest
        {
            CurrencyCode = "USD",
            RateYm = "202607",
            RateValue = 32.5m
        });

        result.Code.ShouldBe("200");
        await _repository.DidNotReceive().UpdateAsync(
            Arg.Any<ExchangeRate>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetStatusAsync_WhenRepositoryThrows_RollsBack()
    {
        _repository.SetStatusAsync(5, "I", "tester", Arg.Any<CancellationToken>())
            .Returns<Task<ExchangeRate?>>(_ => throw new InvalidOperationException("database failure"));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _sut.SetStatusAsync(5, new SetExchangeRateStatusRequest { Status = "I" }));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
