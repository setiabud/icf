using FakeItEasy;
using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.UnitTests.Services.PledgePaymentServiceTests;

public sealed class UpdateAsyncTests
{
    [Fact]
    public async Task UpdateAsync_updates_payment_with_injected_time_provider()
    {
        var fixture = new PledgePaymentServiceTestFixture();
        var payment = PledgePaymentServiceTestFixture.CreatePayment(fixture.Clock);
        var originalCreatedAt = payment.CreatedAt;
        var updatedPledgeId = PledgeId.New();
        fixture.Clock.Advance(TimeSpan.FromMinutes(5));
        A.CallTo(() => fixture.Repository.GetByIdAsync(payment.Id, A<CancellationToken>._)).Returns(payment);

        var result = await fixture.Service.UpdateAsync(
            payment.Id, updatedPledgeId, 25.50m, new DateTime(2026, 1, 20), "Updated", false);

        Assert.Same(payment, result);
        Assert.Equal(updatedPledgeId, payment.PledgeId);
        Assert.Equal(25.50m, payment.Amount);
        Assert.Equal(new DateTime(2026, 1, 20), payment.PaymentDate);
        Assert.Equal("Updated", payment.Comment);
        Assert.False(payment.Active);
        Assert.Equal(originalCreatedAt, payment.CreatedAt);
        Assert.Equal(PledgePaymentServiceTestFixture.StartTime.AddMinutes(5).UtcDateTime, payment.UpdatedAt);
        A.CallTo(() => fixture.Repository.UpdateAsync(payment, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateAsync_when_payment_does_not_exist_returns_null_without_updating()
    {
        var fixture = new PledgePaymentServiceTestFixture();
        var id = PledgePaymentId.New();
        A.CallTo(() => fixture.Repository.GetByIdAsync(id, A<CancellationToken>._)).Returns((PledgePayment?)null);

        var result = await fixture.Service.UpdateAsync(
            id, PledgeId.New(), 10m, PledgePaymentServiceTestFixture.StartTime.UtcDateTime, null, true);

        Assert.Null(result);
        A.CallTo(() => fixture.Repository.UpdateAsync(A<PledgePayment>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task UpdateAsync_with_empty_pledge_id_throws_without_calling_repository()
    {
        var fixture = new PledgePaymentServiceTestFixture();

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Service.UpdateAsync(
            PledgePaymentId.New(), default, 10m, PledgePaymentServiceTestFixture.StartTime.UtcDateTime, null, true));

        Assert.Empty(Fake.GetCalls(fixture.Repository));
    }

    [Fact]
    public async Task UpdateAsync_when_domain_rejects_values_throws_without_persisting_or_changing_payment()
    {
        var fixture = new PledgePaymentServiceTestFixture();
        var payment = PledgePaymentServiceTestFixture.CreatePayment(fixture.Clock);
        A.CallTo(() => fixture.Repository.GetByIdAsync(payment.Id, A<CancellationToken>._)).Returns(payment);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => fixture.Service.UpdateAsync(
            payment.Id, payment.PledgeId, 0m, new DateTime(2026, 1, 20), "Updated", false));

        Assert.Equal(10m, payment.Amount);
        Assert.Equal(new DateTime(2026, 1, 10), payment.PaymentDate);
        Assert.Equal("Original", payment.Comment);
        Assert.True(payment.Active);
        A.CallTo(() => fixture.Repository.UpdateAsync(A<PledgePayment>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task UpdateAsync_forwards_cancellation_token_to_repository()
    {
        var fixture = new PledgePaymentServiceTestFixture();
        var payment = PledgePaymentServiceTestFixture.CreatePayment(fixture.Clock);
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        A.CallTo(() => fixture.Repository.GetByIdAsync(payment.Id, cancellationToken)).Returns(payment);

        await fixture.Service.UpdateAsync(
            payment.Id, payment.PledgeId, 25m, new DateTime(2026, 1, 20), null, true, cancellationToken);

        A.CallTo(() => fixture.Repository.GetByIdAsync(payment.Id, cancellationToken))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => fixture.Repository.UpdateAsync(payment, cancellationToken))
            .MustHaveHappenedOnceExactly();
    }
}