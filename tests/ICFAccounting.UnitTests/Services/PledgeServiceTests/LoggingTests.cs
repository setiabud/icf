using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace ICFAccounting.UnitTests.Services.PledgeServiceTests;

public sealed class LoggingTests
{
    private readonly IPledgeRepository _repository;
    private readonly FakeLogger<PledgeService> _logger;
    private readonly PledgeService _service;

    public LoggingTests()
    {
        var fixture = new PledgeServiceTestFixture();
        _repository = fixture.Repository;
        _logger = fixture.Logger;
        _service = fixture.Service;
    }

    [Fact]
    public async Task AddAsync_logs_added_pledge()
    {
        var pledge = CreatePledge();

        await _service.AddAsync(pledge);

        AssertSingleLog(3001, $"Added pledge {pledge.Id}.");
    }

    [Fact]
    public async Task GetByIdAsync_logs_retrieved_pledge()
    {
        var pledge = CreatePledge();
        A.CallTo(() => _repository.GetByIdAsync(pledge.Id, A<CancellationToken>._)).Returns(pledge);

        await _service.GetByIdAsync(pledge.Id);

        AssertSingleLog(3002, $"Retrieved pledge {pledge.Id}.");
    }

    [Fact]
    public async Task GetByIdAsync_logs_pledge_not_found()
    {
        var id = PledgeId.New();
        A.CallTo(() => _repository.GetByIdAsync(id, A<CancellationToken>._)).Returns((Pledge?)null);

        await _service.GetByIdAsync(id);

        AssertSingleLog(3003, $"Pledge {id} was not found.");
    }

    [Fact]
    public async Task GetByPledgerAsync_logs_retrieved_count()
    {
        var pledgerId = PledgerId.New();
        A.CallTo(() => _repository.GetByPledgerAsync(pledgerId, A<PledgeQuery?>._, A<CancellationToken>._))
            .Returns(PagedPledgesResult.Create([CreatePledge(), CreatePledge()], 1, 20, 2));

        await _service.GetByPledgerAsync(pledgerId);

        AssertSingleLog(3004, $"Retrieved 2 pledges for pledger {pledgerId}.");
    }

    [Fact]
    public async Task GetByCategoryAsync_logs_retrieved_count()
    {
        var categoryId = PledgeCategoryId.New();
        A.CallTo(() => _repository.GetByCategoryAsync(categoryId, A<PledgeQuery?>._, A<CancellationToken>._))
            .Returns(PagedPledgesResult.Create([CreatePledge()], 1, 20, 1));

        await _service.GetByCategoryAsync(categoryId);

        AssertSingleLog(3005, $"Retrieved 1 pledges for category {categoryId}.");
    }

    [Fact]
    public async Task UpdateAsync_logs_updated_pledge()
    {
        var pledge = CreatePledge();
        A.CallTo(() => _repository.GetByIdAsync(pledge.Id, A<CancellationToken>._)).Returns(pledge);

        await _service.UpdateAsync(pledge.Id, 2027, 125m, pledge.PledgerId, pledge.PledgeCategoryId, null);

        AssertSingleLog(3006, $"Updated pledge {pledge.Id}.");
    }

    [Fact]
    public async Task UpdateAsync_logs_pledge_not_found_for_update()
    {
        var id = PledgeId.New();
        A.CallTo(() => _repository.GetByIdAsync(id, A<CancellationToken>._)).Returns((Pledge?)null);

        await _service.UpdateAsync(id, 2027, 125m, PledgerId.New(), PledgeCategoryId.New(), null);

        AssertSingleLog(3007, $"Pledge {id} was not found for update.");
    }

    [Fact]
    public async Task RemoveAsync_logs_removed_pledge()
    {
        var id = PledgeId.New();

        await _service.RemoveAsync(id);

        AssertSingleLog(3008, $"Removed pledge {id}.");
    }

    [Fact]
    public async Task RemoveAsync_when_repository_throws_does_not_log_removal()
    {
        var id = PledgeId.New();
        A.CallTo(() => _repository.RemoveAsync(id, A<CancellationToken>._)).ThrowsAsync(new KeyNotFoundException());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.RemoveAsync(id));

        Assert.Empty(_logger.Collector.GetSnapshot());
    }

    private static Pledge CreatePledge()
    {
        return Pledge.Create(2026, 100m, PledgerId.New(), PledgeCategoryId.New());
    }

    private void AssertSingleLog(int eventId, string message)
    {
        var record = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Equal(eventId, record.Id.Id);
        Assert.Equal(message, record.Message);
    }
}
