using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.UnitTests.Services.PledgerService;

internal sealed class PledgerServiceTestFixture
{
    public PledgerServiceTestFixture()
    {
        Repository = A.Fake<IPledgerRepository>();
        Logger = A.Fake<ILogger<ICFAccounting.Application.Services.PledgerService>>();
        Service = new ICFAccounting.Application.Services.PledgerService(Repository, Logger);
    }

    public IPledgerRepository Repository { get; }

    public ILogger<ICFAccounting.Application.Services.PledgerService> Logger { get; }

    public ICFAccounting.Application.Services.PledgerService Service { get; }

    public static Pledger CreatePledgerWithName(string? name)
    {
        var pledger = Pledger.Create("Community Fund");
        typeof(Pledger).GetProperty(nameof(Pledger.Name))!.SetValue(pledger, name);
        return pledger;
    }
}
