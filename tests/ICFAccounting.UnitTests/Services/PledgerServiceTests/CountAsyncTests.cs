using FakeItEasy;

namespace ICFAccounting.UnitTests.Services.PledgerServiceTests;

public sealed class CountAsyncTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    public async Task CountAsync_returns_repository_count_and_forwards_token(int count)
    {
        var fixture = new PledgerServiceTestFixture();
        using var tokenSource = new CancellationTokenSource();
        A.CallTo(() => fixture.Repository.CountAsync(tokenSource.Token)).Returns(count);

        var result = await fixture.Service.CountAsync(tokenSource.Token);

        Assert.Equal(count, result);
        A.CallTo(() => fixture.Repository.CountAsync(tokenSource.Token))
            .MustHaveHappenedOnceExactly();
    }
}
