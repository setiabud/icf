using FakeItEasy;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.UnitTests.Services.PledgeCategoryServiceTests;

internal sealed class PledgeCategoryServiceTestFixture
{
    public PledgeCategoryServiceTestFixture()
    {
        Repository = A.Fake<IPledgeCategoryRepository>();
        Logger = A.Fake<ILogger<PledgeCategoryService>>();
        Service = new PledgeCategoryService(Repository, Logger);
    }

    public IPledgeCategoryRepository Repository { get; }

    public ILogger<PledgeCategoryService> Logger { get; }

    public PledgeCategoryService Service { get; }

    // PledgeCategory.Create rejects blank names, so reflection is needed to build an invalid entity.
    public static PledgeCategory CreatePledgeCategoryWithName(string? name)
    {
        var pledgeCategory = PledgeCategory.Create("Tithe");
        typeof(PledgeCategory).GetProperty(nameof(PledgeCategory.CategoryName))!.SetValue(pledgeCategory, name);
        return pledgeCategory;
    }
}
