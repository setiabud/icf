using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace ICFAccounting.UnitTests.Services.PledgeCategoryServiceTests;

internal sealed class PledgeCategoryServiceTestFixture
{
    public PledgeCategoryServiceTestFixture()
    {
        Repository = A.Fake<IPledgeCategoryRepository>();
        Logger = A.Fake<ILogger<PledgeCategoryService>>();
        Clock = new FakeTimeProvider(StartTime);
        Service = new PledgeCategoryService(Repository, Logger, Clock);
    }

    public IPledgeCategoryRepository Repository { get; }

    public ILogger<PledgeCategoryService> Logger { get; }

    public static DateTimeOffset StartTime { get; } = new(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);

    public FakeTimeProvider Clock { get; }

    public PledgeCategoryService Service { get; }

    // PledgeCategory.Create rejects blank names, so reflection is needed to build an invalid entity.
    public static PledgeCategory CreatePledgeCategoryWithName(string? name)
    {
        var pledgeCategory = PledgeCategory.Create("Tithe");
        typeof(PledgeCategory).GetProperty(nameof(PledgeCategory.CategoryName))!.SetValue(pledgeCategory, name);
        return pledgeCategory;
    }
}
