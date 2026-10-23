using FakeItEasy;
using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.UnitTests.Services.PledgeCategoryServiceTests;

public sealed class RemoveAsyncTests
{
    [Fact]
    public async Task RemoveAsync_with_valid_id_removes_from_repository()
    {
        var fixture = new PledgeCategoryServiceTestFixture();
        var id = PledgeCategoryId.New();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await fixture.Service.RemoveAsync(id, cancellationToken);

        A.CallTo(() => fixture.Repository.RemoveAsync(id, cancellationToken))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task RemoveAsync_with_empty_id_throws_without_calling_repository()
    {
        var fixture = new PledgeCategoryServiceTestFixture();

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Service.RemoveAsync(default));

        A.CallTo(() => fixture.Repository.RemoveAsync(A<PledgeCategoryId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}
