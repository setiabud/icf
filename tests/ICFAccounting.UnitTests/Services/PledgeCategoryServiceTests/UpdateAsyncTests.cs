using FakeItEasy;
using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.UnitTests.Services.PledgeCategoryServiceTests;

public sealed class UpdateAsyncTests
{
    [Fact]
    public async Task UpdateAsync_with_valid_category_updates_repository()
    {
        var fixture = new PledgeCategoryServiceTestFixture();
        var category = PledgeCategory.Create("Tithe");
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await fixture.Service.UpdateAsync(category, cancellationToken);

        A.CallTo(() => fixture.Repository.UpdateAsync(category, cancellationToken))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateAsync_with_null_category_throws_without_calling_repository()
    {
        var fixture = new PledgeCategoryServiceTestFixture();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => fixture.Service.UpdateAsync(null!));

        A.CallTo(() => fixture.Repository.UpdateAsync(A<PledgeCategory>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}
