using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace ICFAccounting.UnitTests.Services.PledgeCategoryServiceTests;

public sealed class LoggingTests
{
    private readonly IPledgeCategoryRepository _repository = A.Fake<IPledgeCategoryRepository>();
    private readonly FakeLogger<PledgeCategoryService> _logger = new();
    private readonly PledgeCategoryService _service;

    public LoggingTests()
    {
        _service = new PledgeCategoryService(_repository, _logger, TimeProvider.System);
    }

    [Fact]
    public async Task AddAsync_logs_added_category()
    {
        var category = PledgeCategory.Create("Tithe");

        await _service.AddAsync(category);

        AssertSingleLog(2001, $"Added pledge category {category.Id}.");
    }

    [Fact]
    public async Task GetByIdAsync_logs_retrieved_category()
    {
        var category = PledgeCategory.Create("Tithe");
        A.CallTo(() => _repository.GetByIdAsync(category.Id, A<CancellationToken>._)).Returns(category);

        await _service.GetByIdAsync(category.Id);

        AssertSingleLog(2002, $"Retrieved pledge category {category.Id}.");
    }

    [Fact]
    public async Task GetByIdAsync_logs_category_not_found()
    {
        var id = PledgeCategoryId.New();
        A.CallTo(() => _repository.GetByIdAsync(id, A<CancellationToken>._)).Returns((PledgeCategory?)null);

        await _service.GetByIdAsync(id);

        AssertSingleLog(2003, $"Pledge category {id} was not found.");
    }

    [Fact]
    public async Task GetAllAsync_logs_retrieved_count()
    {
        A.CallTo(() => _repository.GetAllAsync(A<CancellationToken>._))
            .Returns([PledgeCategory.Create("Tithe"), PledgeCategory.Create("Missions")]);

        await _service.GetAllAsync();

        AssertSingleLog(2004, "Retrieved 2 pledge categories.");
    }

    [Fact]
    public async Task UpdateAsync_logs_updated_category()
    {
        var category = PledgeCategory.Create("Tithe");
        A.CallTo(() => _repository.GetByIdAsync(category.Id, A<CancellationToken>._)).Returns(category);

        await _service.UpdateAsync(category.Id, "Building Fund", true);

        AssertSingleLog(2005, $"Updated pledge category {category.Id}.");
    }

    [Fact]
    public async Task UpdateAsync_logs_category_not_found_for_update()
    {
        var id = PledgeCategoryId.New();
        A.CallTo(() => _repository.GetByIdAsync(id, A<CancellationToken>._)).Returns((PledgeCategory?)null);

        await _service.UpdateAsync(id, "Building Fund", true);

        AssertSingleLog(2006, $"Pledge category {id} was not found for update.");
    }

    [Fact]
    public async Task RemoveAsync_logs_removed_category()
    {
        var id = PledgeCategoryId.New();

        await _service.RemoveAsync(id);

        AssertSingleLog(2007, $"Removed pledge category {id}.");
    }

    [Fact]
    public async Task RemoveAsync_when_repository_throws_does_not_log_removal()
    {
        var id = PledgeCategoryId.New();
        A.CallTo(() => _repository.RemoveAsync(id, A<CancellationToken>._)).ThrowsAsync(new KeyNotFoundException());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.RemoveAsync(id));

        Assert.Empty(_logger.Collector.GetSnapshot());
    }

    private void AssertSingleLog(int eventId, string message)
    {
        var record = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Equal(eventId, record.Id.Id);
        Assert.Equal(message, record.Message);
    }
}
