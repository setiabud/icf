using FakeItEasy;
using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.UnitTests.Services.PledgeCategoryServiceTests;

public sealed class GetAllAsyncTests
{
    [Fact]
    public async Task GetAllAsync_returns_all_categories_from_repository()
    {
        var fixture = new PledgeCategoryServiceTestFixture();
        var categories = new[] { PledgeCategory.Create("Tithe"), PledgeCategory.Create("Missions") };
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        A.CallTo(() => fixture.Repository.GetAllAsync(cancellationToken))
            .Returns(categories);

        var result = await fixture.Service.GetAllAsync(cancellationToken);

        Assert.Equal(categories, result);
        A.CallTo(() => fixture.Repository.GetAllAsync(cancellationToken))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetAllAsync_when_repository_is_empty_returns_empty_list()
    {
        var fixture = new PledgeCategoryServiceTestFixture();
        A.CallTo(() => fixture.Repository.GetAllAsync(A<CancellationToken>._))
            .Returns(Array.Empty<PledgeCategory>());

        var result = await fixture.Service.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_returns_copy_isolated_from_repository_collection()
    {
        var fixture = new PledgeCategoryServiceTestFixture();
        var tithe = PledgeCategory.Create("Tithe");
        var repositoryCategories = new List<PledgeCategory> { tithe };
        A.CallTo(() => fixture.Repository.GetAllAsync(A<CancellationToken>._))
            .Returns(repositoryCategories);

        var result = await fixture.Service.GetAllAsync();
        repositoryCategories.Add(PledgeCategory.Create("Missions"));

        Assert.NotSame(repositoryCategories, result);
        Assert.Same(tithe, Assert.Single(result));
    }
}
