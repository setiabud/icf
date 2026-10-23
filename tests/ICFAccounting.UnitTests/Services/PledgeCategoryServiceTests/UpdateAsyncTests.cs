using FakeItEasy;
using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.UnitTests.Services.PledgeCategoryServiceTests;

public sealed class UpdateAsyncTests
{
    [Fact]
    public async Task UpdateAsync_with_existing_id_updates_and_returns_category()
    {
        var fixture = new PledgeCategoryServiceTestFixture();
        var category = PledgeCategory.Create("Tithe");
        var originalUpdatedAt = category.UpdatedAt;
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        A.CallTo(() => fixture.Repository.GetByIdAsync(category.Id, cancellationToken))
            .Returns(category);

        var result = await fixture.Service.UpdateAsync(category.Id, "Building Fund", false, cancellationToken);

        Assert.Same(category, result);
        Assert.NotNull(result);
        Assert.Equal("Building Fund", result.CategoryName);
        Assert.False(result.Active);
        Assert.True(result.UpdatedAt >= originalUpdatedAt);
        A.CallTo(() => fixture.Repository.UpdateAsync(category, cancellationToken))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateAsync_when_category_does_not_exist_returns_null_without_updating()
    {
        var fixture = new PledgeCategoryServiceTestFixture();
        var id = PledgeCategoryId.New();
        A.CallTo(() => fixture.Repository.GetByIdAsync(id, A<CancellationToken>._))
            .Returns((PledgeCategory?)null);

        var result = await fixture.Service.UpdateAsync(id, "Building Fund", true);

        Assert.Null(result);
        A.CallTo(() => fixture.Repository.UpdateAsync(A<PledgeCategory>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task UpdateAsync_with_empty_id_throws_without_calling_repository()
    {
        var fixture = new PledgeCategoryServiceTestFixture();

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Service.UpdateAsync(default, "Building Fund", true));

        Assert.Empty(Fake.GetCalls(fixture.Repository));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task UpdateAsync_with_invalid_name_throws_without_calling_repository(string? name)
    {
        var fixture = new PledgeCategoryServiceTestFixture();

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => fixture.Service.UpdateAsync(PledgeCategoryId.New(), name!, true));

        Assert.Empty(Fake.GetCalls(fixture.Repository));
    }
}
