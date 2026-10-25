using FakeItEasy;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.UnitTests.Services.PledgeServiceTests;

public sealed class UpdateAsyncTests
{
    [Fact]
    public async Task UpdateAsync_updates_pledge_with_injected_time_provider()
    {
        var fixture = new PledgeServiceTestFixture();
        var pledge = Pledge.Create(2026, 100m, PledgerId.New(), PledgeCategoryId.New(), "Original", fixture.Clock);
        var originalCreatedAt = pledge.CreatedAt;
        var updatedPledgerId = PledgerId.New();
        var updatedCategoryId = PledgeCategoryId.New();
        fixture.Clock.Advance(TimeSpan.FromMinutes(5));
        A.CallTo(() => fixture.Repository.GetByIdAsync(pledge.Id, A<CancellationToken>._)).Returns(pledge);

        var result = await fixture.Service.UpdateAsync(
            pledge.Id, 2027, 125m, updatedPledgerId, updatedCategoryId, "Updated");

        Assert.Same(pledge, result);
        Assert.Equal(2027, pledge.Year);
        Assert.Equal(125m, pledge.Amount);
        Assert.Equal(updatedPledgerId, pledge.PledgerId);
        Assert.Equal(updatedCategoryId, pledge.PledgeCategoryId);
        Assert.Equal("Updated", pledge.Comment);
        Assert.Equal(originalCreatedAt, pledge.CreatedAt);
        Assert.Equal(PledgeServiceTestFixture.StartTime.AddMinutes(5).UtcDateTime, pledge.UpdatedAt);
        A.CallTo(() => fixture.Repository.UpdateAsync(pledge, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateAsync_when_pledge_does_not_exist_returns_null_without_updating()
    {
        var fixture = new PledgeServiceTestFixture();
        var id = PledgeId.New();
        A.CallTo(() => fixture.Repository.GetByIdAsync(id, A<CancellationToken>._)).Returns((Pledge?)null);

        var result = await fixture.Service.UpdateAsync(id, 2026, 100m, PledgerId.New(), PledgeCategoryId.New(), null);

        Assert.Null(result);
        A.CallTo(() => fixture.Repository.UpdateAsync(A<Pledge>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}