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
    public async Task RemoveAsync_when_category_is_missing_propagates_key_not_found()
    {
        var fixture = new PledgeCategoryServiceTestFixture();
        var id = PledgeCategoryId.New();
        var exception = new KeyNotFoundException("Pledge category was not found.");
        A.CallTo(() => fixture.Repository.RemoveAsync(id, A<CancellationToken>._))
            .ThrowsAsync(exception);

        var result = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => fixture.Service.RemoveAsync(id));

        Assert.Same(exception, result);
    }

    [Fact]
    public async Task RemoveAsync_when_repository_cancels_propagates_cancellation()
    {
        var fixture = new PledgeCategoryServiceTestFixture();
        var id = PledgeCategoryId.New();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        A.CallTo(() => fixture.Repository.RemoveAsync(id, cancellationToken))
            .ThrowsAsync(new OperationCanceledException(cancellationToken));

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(
            () => fixture.Service.RemoveAsync(id, cancellationToken));

        Assert.Equal(cancellationToken, exception.CancellationToken);
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
