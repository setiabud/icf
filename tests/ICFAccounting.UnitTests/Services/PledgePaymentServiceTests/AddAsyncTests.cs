using FakeItEasy;

namespace ICFAccounting.UnitTests.Services.PledgePaymentServiceTests;

public sealed class AddAsyncTests
{
    [Fact]
    public async Task AddAsync_forwards_payment_and_cancellation_token()
    {
        var fixture = new PledgePaymentServiceTestFixture();
        var payment = PledgePaymentServiceTestFixture.CreatePayment();
        using var cancellationTokenSource = new CancellationTokenSource();

        await fixture.Service.AddAsync(payment, cancellationTokenSource.Token);

        A.CallTo(() => fixture.Repository.AddAsync(payment, cancellationTokenSource.Token))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AddAsync_with_null_payment_throws_without_calling_repository()
    {
        var fixture = new PledgePaymentServiceTestFixture();

        await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.AddAsync(null!));

        A.CallTo(() => fixture.Repository.AddAsync(A<ICFAccounting.Domain.PledgePayments.PledgePayment>._,
                A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task AddAsync_when_repository_fails_propagates_exception()
    {
        var fixture = new PledgePaymentServiceTestFixture();
        var payment = PledgePaymentServiceTestFixture.CreatePayment();
        var exception = new InvalidOperationException("Repository failure.");
        A.CallTo(() => fixture.Repository.AddAsync(payment, A<CancellationToken>._))
            .ThrowsAsync(exception);

        var result = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.AddAsync(payment));

        Assert.Same(exception, result);
    }
}