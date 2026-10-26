using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace ICFAccounting.UnitTests.Services.PledgePaymentServiceTests;

internal sealed class PledgePaymentServiceTestFixture
{
    public PledgePaymentServiceTestFixture()
    {
        Repository = A.Fake<IPledgePaymentRepository>();
        Logger = new FakeLogger<PledgePaymentService>();
        Clock = new FakeTimeProvider(StartTime);
        Service = new PledgePaymentService(Repository, Logger, Clock);
    }

    public IPledgePaymentRepository Repository { get; }

    public FakeLogger<PledgePaymentService> Logger { get; }

    public static DateTimeOffset StartTime { get; } = new(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);

    public FakeTimeProvider Clock { get; }

    public PledgePaymentService Service { get; }

    public static PledgePayment CreatePayment(TimeProvider? clock = null)
    {
        return PledgePayment.Create(PledgeId.New(), 10m, new DateTime(2026, 1, 10), "Original", clock);
    }
}