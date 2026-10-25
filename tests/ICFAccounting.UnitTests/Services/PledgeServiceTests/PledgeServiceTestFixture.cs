using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace ICFAccounting.UnitTests.Services.PledgeServiceTests;

internal sealed class PledgeServiceTestFixture
{
    public PledgeServiceTestFixture()
    {
        Repository = A.Fake<IPledgeRepository>();
        Logger = A.Fake<ILogger<PledgeService>>();
        Clock = new FakeTimeProvider(StartTime);
        Service = new PledgeService(Repository, Logger, Clock);
    }

    public IPledgeRepository Repository { get; }

    public ILogger<PledgeService> Logger { get; }

    public static DateTimeOffset StartTime { get; } = new(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);

    public FakeTimeProvider Clock { get; }

    public PledgeService Service { get; }
}