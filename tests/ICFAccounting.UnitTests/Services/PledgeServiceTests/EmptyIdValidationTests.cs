using FakeItEasy;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.UnitTests.Services.PledgeServiceTests;

public sealed class EmptyIdValidationTests
{
    [Fact]
    public async Task GetByIdAsync_with_empty_id_throws_without_calling_repository()
    {
        var fixture = new PledgeServiceTestFixture();

        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Service.GetByIdAsync(default));

        A.CallTo(() => fixture.Repository.GetByIdAsync(A<PledgeId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}