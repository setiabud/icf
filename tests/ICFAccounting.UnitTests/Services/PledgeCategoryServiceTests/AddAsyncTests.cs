using FakeItEasy;
using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.UnitTests.Services.PledgeCategoryServiceTests;

public sealed class AddAsyncTests
{
    [Fact]
    public async Task AddAsync_with_valid_category_adds_to_repository()
    {
        var fixture = new PledgeCategoryServiceTestFixture();
        var category = PledgeCategory.Create("Tithe");
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await fixture.Service.AddAsync(category, cancellationToken);

        A.CallTo(() => fixture.Repository.AddAsync(category, cancellationToken))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AddAsync_with_null_category_throws_without_calling_repository()
    {
        var fixture = new PledgeCategoryServiceTestFixture();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => fixture.Service.AddAsync(null!));

        A.CallTo(() => fixture.Repository.AddAsync(A<PledgeCategory>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task AddAsync_with_invalid_name_throws_without_calling_repository(string? name)
    {
        var fixture = new PledgeCategoryServiceTestFixture();
        var category = PledgeCategoryServiceTestFixture.CreatePledgeCategoryWithName(name);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => fixture.Service.AddAsync(category));

        A.CallTo(() => fixture.Repository.AddAsync(A<PledgeCategory>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}
