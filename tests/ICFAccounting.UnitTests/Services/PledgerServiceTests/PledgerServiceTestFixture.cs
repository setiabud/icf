using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace ICFAccounting.UnitTests.Services.PledgerServiceTests;

internal sealed class PledgerServiceTestFixture
{
    public PledgerServiceTestFixture()
    {
        Repository = A.Fake<IPledgerRepository>();
        Logger = A.Fake<ILogger<PledgerService>>();
        Clock = new FakeTimeProvider(StartTime);
        Service = new PledgerService(Repository, Logger, Clock);
    }

    public IPledgerRepository Repository { get; }

    public ILogger<PledgerService> Logger { get; }

    public static DateTimeOffset StartTime { get; } = new(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);

    public FakeTimeProvider Clock { get; }

    public PledgerService Service { get; }

    public static Pledger CreatePledgerWithName(string? name)
    {
        var pledger = Pledger.Create("Community Fund");
        typeof(Pledger).GetProperty(nameof(Pledger.PledgerName))!.SetValue(pledger, name);
        return pledger;
    }
}
