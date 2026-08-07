using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Mapping;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Dealer;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Application.Services;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Tests.Services;

public class DealerServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IDealerRepository _dealerRepo = Substitute.For<IDealerRepository>();
    private readonly IRegionRepository _regionRepo = Substitute.For<IRegionRepository>();
    private readonly IDealerMapper _mapper = new DealerMapper();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly DealerService _sut;

    public DealerServiceTests()
    {
        _uow.Dealers.Returns(_dealerRepo);
        _uow.Regions.Returns(_regionRepo);
        _currentUser.UserId.Returns("tester");
        _requestContext.TraceId.Returns("dealer-trace");
        _sut = new DealerService(_uow, _mapper, _currentUser, _requestContext);
    }

    private static Region ActiveRegion(int regionId = 1) =>
        new() { RegionId = regionId, RegionCode = "NORTH", RegionName = "北區", Status = "A" };

    private static Dealer SampleDealer(int dealerId = 1) => new()
    {
        DealerId = dealerId,
        DealerCode = "D001",
        DealerName = "台北經銷商",
        RegionId = 1,
        RegionName = "北區",
        CurrencyCode = "TWD",
        Status = "A"
    };

    private static DealerSaveRequest ValidRequest() => new()
    {
        DealerCode = " d001 ",
        DealerName = " 台北經銷商 ",
        RegionId = 1,
        CurrencyCode = "twd"
    };

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPage()
    {
        var filter = new DealerFilter { Page = 2, PageSize = 10 };
        _dealerRepo.GetPagedAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Dealer>
            {
                Datas = [SampleDealer()],
                TotalRow = 11,
                Page = 2,
                PageSize = 10
            });

        var result = await _sut.GetPagedAsync(filter);

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.Datas.Single().DealerName.ShouldBe("台北經銷商");
        result.Data.Datas.Single().RegionName.ShouldBe("北區");
        result.Data.TotalRow.ShouldBe(11);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNullData()
    {
        _dealerRepo.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Dealer?)null);

        var result = await _sut.GetByIdAsync(99);

        result.Code.ShouldBe("100");
        result.Data.ShouldBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CommitsAndReturnsCreatedDealer()
    {
        _regionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ActiveRegion());
        _dealerRepo.AddAsync(Arg.Any<Dealer>(), Arg.Any<CancellationToken>()).Returns(7);
        _dealerRepo.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(SampleDealer(7));

        var result = await _sut.CreateAsync(ValidRequest());

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.DealerId.ShouldBe(7);
        await _uow.Received(1).BeginTransactionAsync(Arg.Any<System.Data.IsolationLevel>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _dealerRepo.Received(1).AddAsync(
            Arg.Is<Dealer>(x =>
                x.DealerCode == "D001" &&
                x.DealerName == "台北經銷商" &&
                x.CurrencyCode == "TWD" &&
                x.Status == "A" &&
                x.CrtUser == "tester"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenCodeExists_ReturnsValidationError()
    {
        _dealerRepo.ExistsCodeAsync("D001", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(ValidRequest());

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("已存在");
        await _dealerRepo.DidNotReceive().AddAsync(Arg.Any<Dealer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenRegionInactive_ReturnsValidationError()
    {
        _regionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Region { RegionId = 1, RegionCode = "NORTH", RegionName = "北區", Status = "I" });

        var result = await _sut.CreateAsync(ValidRequest());

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("區域");
        await _dealerRepo.DidNotReceive().AddAsync(Arg.Any<Dealer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCurrencyCode_ReturnsValidationError()
    {
        var request = ValidRequest();
        request.CurrencyCode = "TW1";

        var result = await _sut.CreateAsync(request);

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("幣別");
        await _dealerRepo.DidNotReceive().AddAsync(Arg.Any<Dealer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenAddFails_RollsBack()
    {
        _regionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ActiveRegion());
        _dealerRepo.AddAsync(Arg.Any<Dealer>(), Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new InvalidOperationException("db error"));

        await Should.ThrowAsync<InvalidOperationException>(() => _sut.CreateAsync(ValidRequest()));

        await _uow.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsValidationError()
    {
        _dealerRepo.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Dealer?)null);

        var result = await _sut.UpdateAsync(99, ValidRequest());

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("找不到");
        await _dealerRepo.DidNotReceive().UpdateAsync(Arg.Any<Dealer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_CommitsChange()
    {
        _dealerRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(SampleDealer());
        _regionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ActiveRegion());

        var request = ValidRequest();
        request.DealerName = "新名稱";

        var result = await _sut.UpdateAsync(1, request);

        result.Code.ShouldBe("100");
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _dealerRepo.Received(1).UpdateAsync(
            Arg.Is<Dealer>(x => x.DealerName == "新名稱" && x.MdfUser == "tester"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStatusAsync_WithInvalidStatus_ReturnsValidationError()
    {
        var result = await _sut.UpdateStatusAsync(1, new DealerStatusRequest { Status = "X" });

        result.Code.ShouldBe("200");
        await _dealerRepo.DidNotReceive().UpdateStatusAsync(Arg.Any<Dealer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenEnablingWithInactiveRegion_ReturnsValidationError()
    {
        var dealer = SampleDealer();
        dealer.Status = "I";
        _dealerRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(dealer);
        _regionRepo.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Region { RegionId = 1, RegionCode = "NORTH", RegionName = "北區", Status = "I" });

        var result = await _sut.UpdateStatusAsync(1, new DealerStatusRequest { Status = "A" });

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("區域");
        await _dealerRepo.DidNotReceive().UpdateStatusAsync(Arg.Any<Dealer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenValid_CommitsChange()
    {
        _dealerRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(SampleDealer());
        _dealerRepo.UpdateStatusAsync(Arg.Any<Dealer>(), Arg.Any<CancellationToken>()).Returns(1);

        var result = await _sut.UpdateStatusAsync(1, new DealerStatusRequest { Status = "I" });

        result.Code.ShouldBe("100");
        result.Data.ShouldBeTrue();
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _dealerRepo.Received(1).UpdateStatusAsync(
            Arg.Is<Dealer>(x => x.Status == "I" && x.MdfUser == "tester"),
            Arg.Any<CancellationToken>());
    }
}
