using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace ICFAccounting.UnitTests.Services.PledgePaymentServiceTests;

public sealed class LoggingTests
{
    private readonly IPledgePaymentRepository _repository;
    private readonly FakeLogger<PledgePaymentService> _logger;
    private readonly PledgePaymentService _service;

    public LoggingTests()
    {
        var fixture = new PledgePaymentServiceTestFixture();
        _repository = fixture.Repository;
        _logger = fixture.Logger;
        _service = fixture.Service;
    }

    [Fact]
    public async Task AddAsync_logs_added_payment()
    {
        var payment = PledgePaymentServiceTestFixture.CreatePayment();

        await _service.AddAsync(payment);

        AssertSingleLog(4001, $"Added pledge payment {payment.Id}.");
    }

    [Fact]
    public async Task GetByIdAsync_logs_retrieved_payment()
    {
        var payment = PledgePaymentServiceTestFixture.CreatePayment();
        A.CallTo(() => _repository.GetByIdAsync(payment.Id, A<CancellationToken>._)).Returns(payment);

        await _service.GetByIdAsync(payment.Id);

        AssertSingleLog(4002, $"Retrieved pledge payment {payment.Id}.");
    }

    [Fact]
    public async Task GetByIdAsync_logs_payment_not_found()
    {
        var id = PledgePaymentId.New();
        A.CallTo(() => _repository.GetByIdAsync(id, A<CancellationToken>._)).Returns((PledgePayment?)null);

        await _service.GetByIdAsync(id);

        AssertSingleLog(4003, $"Pledge payment {id} was not found.");
    }

    [Fact]
    public async Task GetByPledgeAsync_logs_retrieved_count()
    {
        var pledgeId = PledgeId.New();
        A.CallTo(() => _repository.GetByPledgeAsync(pledgeId, A<PledgePaymentQuery?>._, A<CancellationToken>._))
            .Returns(PagedPledgePaymentsResult.Create(
                [PledgePaymentServiceTestFixture.CreatePayment(), PledgePaymentServiceTestFixture.CreatePayment()],
                1,
                20,
                2));

        await _service.GetByPledgeAsync(pledgeId);

        AssertSingleLog(4004, $"Retrieved 2 payments for pledge {pledgeId}.");
    }

    [Fact]
    public async Task UpdateAsync_logs_updated_payment()
    {
        var payment = PledgePaymentServiceTestFixture.CreatePayment();
        A.CallTo(() => _repository.GetByIdAsync(payment.Id, A<CancellationToken>._)).Returns(payment);

        await _service.UpdateAsync(payment.Id, payment.PledgeId, 25m, new DateTime(2026, 1, 20), null, true);

        AssertSingleLog(4005, $"Updated pledge payment {payment.Id}.");
    }

    [Fact]
    public async Task UpdateAsync_logs_payment_not_found_for_update()
    {
        var id = PledgePaymentId.New();
        A.CallTo(() => _repository.GetByIdAsync(id, A<CancellationToken>._)).Returns((PledgePayment?)null);

        await _service.UpdateAsync(id, PledgeId.New(), 25m, new DateTime(2026, 1, 20), null, true);

        AssertSingleLog(4006, $"Pledge payment {id} was not found for update.");
    }

    [Fact]
    public async Task RemoveAsync_logs_removed_payment()
    {
        var id = PledgePaymentId.New();

        await _service.RemoveAsync(id);

        AssertSingleLog(4007, $"Removed pledge payment {id}.");
    }

    [Fact]
    public async Task RemoveAsync_when_repository_throws_does_not_log_removal()
    {
        var id = PledgePaymentId.New();
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
