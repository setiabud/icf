using FakeItEasy;
using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.UnitTests.Services.PledgerServiceTests;

public sealed class AddAsyncTests
{
    [Fact]
    public async Task AddAsync_with_pledger_adds_it_to_repository()
    {
        var fixture = new PledgerServiceTestFixture();
        var pledger = Pledger.Create("Community Fund");
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await fixture.Service.AddAsync(pledger, cancellationToken);

        A.CallTo(() => fixture.Repository.AddAsync(pledger, cancellationToken))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AddAsync_with_null_pledger_throws_without_calling_repository()
    {
        var fixture = new PledgerServiceTestFixture();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => fixture.Service.AddAsync(null!));

        A.CallTo(() => fixture.Repository.AddAsync(A<Pledger>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task AddAsync_with_invalid_name_throws_without_calling_repository(string? name)
    {
        var fixture = new PledgerServiceTestFixture();
        var pledger = PledgerServiceTestFixture.CreatePledgerWithName(name);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => fixture.Service.AddAsync(pledger));

        A.CallTo(() => fixture.Repository.AddAsync(A<Pledger>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}
