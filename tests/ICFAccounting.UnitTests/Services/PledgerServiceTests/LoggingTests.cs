using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace ICFAccounting.UnitTests.Services.PledgerServiceTests;

public sealed class LoggingTests
{
    private readonly IPledgerRepository _repository = A.Fake<IPledgerRepository>();
    private readonly FakeLogger<PledgerService> _logger = new();
    private readonly PledgerService _service;

    public LoggingTests()
    {
        _service = new PledgerService(_repository, _logger, TimeProvider.System);
    }

    [Fact]
    public async Task AddAsync_logs_added_pledger()
    {
        var pledger = Pledger.Create("Community Fund");

        await _service.AddAsync(pledger);

        AssertSingleLog(1001, $"Added pledger {pledger.Id}.");
    }

    [Fact]
    public async Task GetByIdAsync_logs_retrieved_pledger()
    {
        var pledger = Pledger.Create("Community Fund");
        A.CallTo(() => _repository.GetByIdAsync(pledger.Id, A<CancellationToken>._)).Returns(pledger);

        await _service.GetByIdAsync(pledger.Id);

        AssertSingleLog(1002, $"Retrieved pledger {pledger.Id}.");
    }

    [Fact]
    public async Task GetByIdAsync_logs_pledger_not_found()
    {
        var id = PledgerId.New();
        A.CallTo(() => _repository.GetByIdAsync(id, A<CancellationToken>._)).Returns((Pledger?)null);

        await _service.GetByIdAsync(id);

        AssertSingleLog(1003, $"Pledger {id} was not found.");
    }

    [Fact]
    public async Task GetAllAsync_logs_retrieved_count()
    {
        IReadOnlyList<Pledger> pledgers = [Pledger.Create("A"), Pledger.Create("B")];
        A.CallTo(() => _repository.GetAllAsync(A<PledgerQuery?>._, A<CancellationToken>._))
            .Returns(PagedPledgersResult.Create(pledgers, 1, 20, 2));

        await _service.GetAllAsync();

        AssertSingleLog(1004, "Retrieved 2 pledgers.");
    }

    [Fact]
    public async Task UpdateAsync_logs_updated_pledger()
    {
        var pledger = Pledger.Create("Community Fund");
        A.CallTo(() => _repository.GetByIdAsync(pledger.Id, A<CancellationToken>._)).Returns(pledger);

        await _service.UpdateAsync(pledger.Id, "Mission Fund", true);

        AssertSingleLog(1005, $"Updated pledger {pledger.Id}.");
    }

    [Fact]
    public async Task UpdateAsync_logs_pledger_not_found_for_update()
    {
        var id = PledgerId.New();
        A.CallTo(() => _repository.GetByIdAsync(id, A<CancellationToken>._)).Returns((Pledger?)null);

        await _service.UpdateAsync(id, "Mission Fund", true);

        AssertSingleLog(1006, $"Pledger {id} was not found for update.");
    }

    [Fact]
    public async Task RemoveAsync_logs_removed_pledger()
    {
        var id = PledgerId.New();

        await _service.RemoveAsync(id);

        AssertSingleLog(1007, $"Removed pledger {id}.");
    }

    [Fact]
    public async Task RemoveAsync_when_repository_throws_does_not_log_removal()
    {
        var id = PledgerId.New();
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
