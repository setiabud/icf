using FakeItEasy;
using ICFAccounting.Domain.PledgePayments;

namespace ICFAccounting.UnitTests.Services.PledgePaymentServiceTests;

public sealed class GetByIdAsyncTests
{
    [Fact]
    public async Task GetByIdAsync_returns_payment_from_repository()
    {
        var fixture = new PledgePaymentServiceTestFixture();
        var payment = PledgePaymentServiceTestFixture.CreatePayment();
        A.CallTo(() => fixture.Repository.GetByIdAsync(payment.Id, A<CancellationToken>._)).Returns(payment);

        var result = await fixture.Service.GetByIdAsync(payment.Id);

        Assert.Same(payment, result);
    }
}