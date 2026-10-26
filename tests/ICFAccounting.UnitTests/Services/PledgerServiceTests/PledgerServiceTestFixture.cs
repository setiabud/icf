using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace ICFAccounting.UnitTests.Services.PledgerServiceTests;

internal sealed class PledgerServiceTestFixture
{
    public PledgerServiceTestFixture()
    {
        Repository = A.Fake<IPledgerRepository>();
        Logger = new FakeLogger<PledgerService>();
        Clock = new FakeTimeProvider(StartTime);
        Service = new PledgerService(Repository, Logger, Clock);
    }

    public IPledgerRepository Repository { get; }

    public FakeLogger<PledgerService> Logger { get; }

    public static DateTimeOffset StartTime { get; } = new(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);

    public FakeTimeProvider Clock { get; }

    public PledgerService Service { get; }
}
