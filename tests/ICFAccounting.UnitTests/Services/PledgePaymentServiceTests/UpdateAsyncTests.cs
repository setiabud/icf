using FakeItEasy;
using ICFAccounting.Domain.PledgePayments;

namespace ICFAccounting.UnitTests.Services.PledgePaymentServiceTests;

public sealed class UpdateAsyncTests
{
    [Fact]
    public async Task UpdateAsync_updates_payment_with_injected_time_provider()
    {
        var fixture = new PledgePaymentServiceTestFixture();
        var payment = PledgePaymentServiceTestFixture.CreatePayment(fixture.Clock);
        var originalCreatedAt = payment.CreatedAt;
        var originalPledgeId = payment.PledgeId;
        fixture.Clock.Advance(TimeSpan.FromMinutes(5));
        A.CallTo(() => fixture.Repository.GetByIdAsync(payment.Id, A<CancellationToken>._)).Returns(payment);

        var result = await fixture.Service.UpdateAsync(
            payment.Id, 25.50m, new DateTime(2026, 1, 20), "Updated", false);

        Assert.Same(payment, result);
        Assert.Equal(originalPledgeId, payment.PledgeId);
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

        var result = await fixture.Service.UpdateAsync(id, 10m, PledgePaymentServiceTestFixture.StartTime.UtcDateTime,
            null, true);

        Assert.Null(result);
        A.CallTo(() => fixture.Repository.UpdateAsync(A<PledgePayment>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}