using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace ICFAccounting.UnitTests.Services.PledgeCategoryServiceTests;

internal sealed class PledgeCategoryServiceTestFixture
{
    public PledgeCategoryServiceTestFixture()
    {
        Repository = A.Fake<IPledgeCategoryRepository>();
        Logger = new FakeLogger<PledgeCategoryService>();
        Clock = new FakeTimeProvider(StartTime);
        Service = new PledgeCategoryService(Repository, Logger, Clock);
    }

    public IPledgeCategoryRepository Repository { get; }

    public FakeLogger<PledgeCategoryService> Logger { get; }

    public static DateTimeOffset StartTime { get; } = new(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);

    public FakeTimeProvider Clock { get; }

    public PledgeCategoryService Service { get; }
}
