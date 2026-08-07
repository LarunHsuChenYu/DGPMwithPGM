using Microsoft.Extensions.Caching.Memory;
using DGPM_SPM.Core.Application.Interfaces;
using DGPM_SPM.Core.Application.Mapping;
using DGPM_SPM.Core.Application.Services;
using DGPM_SPM.Core.Domain.Entities;

namespace DGPM_SPM.Core.Tests.Services;

public class ParameterServiceTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IParameterRepository _paramRepo = Substitute.For<IParameterRepository>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly IParameterMapper _mapper = new ParameterMapper();
    private readonly IRequestContext _requestContext = Substitute.For<IRequestContext>();
    private readonly ParameterService _sut;

    public ParameterServiceTests()
    {
        _uow.Parameters.Returns(_paramRepo);
        _requestContext.TraceId.Returns("test-trace");
        _sut = new ParameterService(_uow, _cache, _mapper, _requestContext);
    }

    [Fact]
    public async Task GetParameterListAsync_ReturnsMappedItems()
    {
        _paramRepo.GetAllByItemAsync("STATUS", Arg.Any<CancellationToken>())
            .Returns(new List<Parameter>
            {
                new() { SetItem = "STATUS", SetType = "A", SetValue = "Active", SortOrder = 1 },
                new() { SetItem = "STATUS", SetType = "I", SetValue = "Inactive", SortOrder = 2 }
            });

        var result = await _sut.GetParameterListAsync("STATUS");

        result.Data.ShouldNotBeNull();
        result.Data.Count.ShouldBe(2);
        result.Data[0].SetValue.ShouldBe("Active");
    }

    [Fact]
    public async Task GetParameterListAsync_UsesCacheOnSecondCall()
    {
        _paramRepo.GetAllByItemAsync("TYPE", Arg.Any<CancellationToken>())
            .Returns(new List<Parameter>
            {
                new() { SetItem = "TYPE", SetType = "X", SetValue = "One", SortOrder = 1 }
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
}
