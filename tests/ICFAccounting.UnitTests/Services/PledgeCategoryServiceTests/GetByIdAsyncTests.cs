using FakeItEasy;
using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.UnitTests.Services.PledgeCategoryServiceTests;

public sealed class GetByIdAsyncTests
{
    [Fact]
    public async Task GetByIdAsync_with_existing_id_returns_category()
    {
        var fixture = new PledgeCategoryServiceTestFixture();
        var category = PledgeCategory.Create("Tithe");
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        A.CallTo(() => fixture.Repository.GetByIdAsync(category.Id, cancellationToken))
            .Returns(category);

        var result = await fixture.Service.GetByIdAsync(category.Id, cancellationToken);

        Assert.Same(category, result);
        A.CallTo(() => fixture.Repository.GetByIdAsync(category.Id, cancellationToken))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetByIdAsync_when_category_does_not_exist_returns_null()
    {
        var fixture = new PledgeCategoryServiceTestFixture();
        var id = PledgeCategoryId.New();
        A.CallTo(() => fixture.Repository.GetByIdAsync(id, A<CancellationToken>._))
            .Returns((PledgeCategory?)null);

        var result = await fixture.Service.GetByIdAsync(id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_with_empty_id_throws_without_calling_repository()
    {
        var fixture = new PledgeCategoryServiceTestFixture();

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Service.GetByIdAsync(default));

        A.CallTo(() => fixture.Repository.GetByIdAsync(A<PledgeCategoryId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}
