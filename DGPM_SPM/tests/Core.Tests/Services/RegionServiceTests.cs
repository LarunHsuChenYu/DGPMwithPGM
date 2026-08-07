using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Mapping;
using DGPM_SPM.Core.Application.Models;
using DGPM_SPM.Core.Application.Models.Region;
using DGPM_SPM.Core.Application.Queries;
using DGPM_SPM.Core.Application.Services;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Tests.Services;

public class RegionServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IRegionRepository _repository = Substitute.For<IRegionRepository>();
    private readonly IRegionMapper _mapper = new RegionMapper();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly RegionService _sut;

    public RegionServiceTests()
    {
        _uow.Regions.Returns(_repository);
        _currentUser.UserId.Returns("tester");
        _requestContext.TraceId.Returns("region-trace");
        _sut = new RegionService(_uow, _mapper, _currentUser, _requestContext);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPage()
    {
        var filter = new RegionFilter { Page = 2, PageSize = 10 };
        _repository.GetPagedAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Region>
            {
                Datas = [new Region { RegionId = 1, RegionCode = "TW", RegionName = "台灣", Status = "A" }],
                TotalRow = 11,
                Page = 2,
                PageSize = 10
            });

        var result = await _sut.GetPagedAsync(filter);

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.Datas.Single().RegionName.ShouldBe("台灣");
        result.Data.TotalRow.ShouldBe(11);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CommitsAndReturnsCreatedRegion()
    {
        var request = new RegionSaveRequest
        {
            RegionCode = " tw ",
            RegionName = " 台灣 ",
            SortOrder = 1
        };
        _repository.AddAsync(Arg.Any<Region>(), Arg.Any<CancellationToken>()).Returns(7);
        _repository.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(new Region { RegionId = 7, RegionCode = "TW", RegionName = "台灣", Status = "A" });

        var result = await _sut.CreateAsync(request);

        result.Code.ShouldBe("100");
        result.Data.ShouldNotBeNull();
        result.Data.RegionId.ShouldBe(7);
        await _uow.Received(1).BeginTransactionAsync(Arg.Any<System.Data.IsolationLevel>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().RollbackAsync(Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAsync(
            Arg.Is<Region>(x => x.RegionCode == "TW" && x.CrtUser == "tester" && x.Status == "A"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenParentIsDescendant_ReturnsValidationError()
    {
        var request = new RegionSaveRequest
        {
            RegionCode = "ROOT",
            RegionName = "根區域",
            ParentRegionId = 3
        };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(new Region { RegionId = 1, RegionCode = "ROOT", RegionName = "根區域", Status = "A" });
        _repository.GetByIdAsync(3, Arg.Any<CancellationToken>())
            .Returns(new Region { RegionId = 3, RegionCode = "CHILD", RegionName = "子區域", Status = "A" });
        _repository.IsDescendantAsync(1, 3, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.UpdateAsync(1, request);

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("下層節點");
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Region>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenActiveDealerExists_DoesNotDisable()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(new Region { RegionId = 5, RegionCode = "NORTH", RegionName = "北區", Status = "A" });
        _repository.HasActiveDealersAsync(5, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.UpdateStatusAsync(5, new RegionStatusRequest { Status = "I" });

        result.Code.ShouldBe("200");
        result.Message.ShouldContain("經銷商");
        await _repository.DidNotReceive().UpdateStatusAsync(Arg.Any<Region>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenValid_CommitsChange()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>())
            .Returns(new Region { RegionId = 5, RegionCode = "NORTH", RegionName = "北區", Status = "A" });
        _repository.UpdateStatusAsync(Arg.Any<Region>(), Arg.Any<CancellationToken>()).Returns(1);

        var result = await _sut.UpdateStatusAsync(5, new RegionStatusRequest { Status = "I" });

        result.Code.ShouldBe("100");
        result.Data.ShouldBeTrue();
        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _repository.Received(1).UpdateStatusAsync(
            Arg.Is<Region>(x => x.Status == "I" && x.MdfUser == "tester"),
            Arg.Any<CancellationToken>());
    }
}
