using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.UnitTests.Repositories;

public sealed class InMemoryPledgeCategoryRepositoryTests
{
    [Fact]
    public async Task AddAsync_then_GetByIdAsync_returns_category()
    {
        var repository = new InMemoryPledgeCategoryRepository();
        var category = PledgeCategory.Create("Community Fund");

        await repository.AddAsync(category);

        Assert.Same(category, await repository.GetByIdAsync(category.Id));
    }

    [Fact]
    public async Task GetByIdAsync_with_unknown_id_returns_null()
    {
        var repository = new InMemoryPledgeCategoryRepository();
        await repository.AddAsync(PledgeCategory.Create("Community Fund"));

        Assert.Null(await repository.GetByIdAsync(PledgeCategoryId.New()));
    }

    [Fact]
    public async Task UpdateAsync_with_unknown_category_adds_it()
    {
        var repository = new InMemoryPledgeCategoryRepository();
        var category = PledgeCategory.Create("Community Fund");

        await repository.UpdateAsync(category);

        Assert.Same(category, await repository.GetByIdAsync(category.Id));
    }

    [Fact]
    public async Task RemoveAsync_deletes_only_requested_category()
    {
        var repository = new InMemoryPledgeCategoryRepository();
        var category = PledgeCategory.Create("Community Fund");
        var otherCategory = PledgeCategory.Create("Building Fund");
        await repository.AddAsync(category);
        await repository.AddAsync(otherCategory);

        await repository.RemoveAsync(category.Id);

        Assert.Null(await repository.GetByIdAsync(category.Id));
        Assert.Same(otherCategory, Assert.Single(await repository.GetAllAsync()));
    }

    [Fact]
    public async Task RemoveAsync_with_unknown_id_throws_without_removing_categories()
    {
        var repository = new InMemoryPledgeCategoryRepository();
        var category = PledgeCategory.Create("Community Fund");
        await repository.AddAsync(category);
        var unknownId = PledgeCategoryId.New();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => repository.RemoveAsync(unknownId));

        Assert.Equal($"PledgeCategory {unknownId.Value} was not found.", exception.Message);
        Assert.Same(category, Assert.Single(await repository.GetAllAsync()));
    }

    [Fact]
    public async Task GetAllAsync_returns_snapshot_unaffected_by_later_additions_and_removals()
    {
        var repository = new InMemoryPledgeCategoryRepository();
        var category = PledgeCategory.Create("Community Fund");
        var otherCategory = PledgeCategory.Create("Building Fund");
        await repository.AddAsync(category);

        var snapshot = await repository.GetAllAsync();
        await repository.AddAsync(otherCategory);

        Assert.Same(category, Assert.Single(snapshot));

        await repository.RemoveAsync(category.Id);

        Assert.Same(category, Assert.Single(snapshot));
        Assert.Same(otherCategory, Assert.Single(await repository.GetAllAsync()));
    }

    [Fact]
    public async Task GetAllAsync_with_empty_repository_returns_empty_list()
    {
        var repository = new InMemoryPledgeCategoryRepository();

        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task AddAsync_with_canceled_token_throws_without_adding_category()
    {
        var repository = new InMemoryPledgeCategoryRepository();
        var category = PledgeCategory.Create("Community Fund");
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.AddAsync(category, tokenSource.Token));

        Assert.Equal(tokenSource.Token, exception.CancellationToken);
        Assert.Null(await repository.GetByIdAsync(category.Id));
        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task GetByIdAsync_with_canceled_token_throws()
    {
        var repository = new InMemoryPledgeCategoryRepository();
        var category = PledgeCategory.Create("Community Fund");
        await repository.AddAsync(category);
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByIdAsync(category.Id, tokenSource.Token));

        Assert.Equal(tokenSource.Token, exception.CancellationToken);
    }

    [Fact]
    public async Task UpdateAsync_with_canceled_token_throws_without_inserting_category()
    {
        var repository = new InMemoryPledgeCategoryRepository();
        var category = PledgeCategory.Create("Community Fund");
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.UpdateAsync(category, tokenSource.Token));

        Assert.Equal(tokenSource.Token, exception.CancellationToken);
        Assert.Null(await repository.GetByIdAsync(category.Id));
        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task RemoveAsync_with_canceled_token_throws_without_removing_category()
    {
        var repository = new InMemoryPledgeCategoryRepository();
        var category = PledgeCategory.Create("Community Fund");
        await repository.AddAsync(category);
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.RemoveAsync(category.Id, tokenSource.Token));

        Assert.Equal(tokenSource.Token, exception.CancellationToken);
        Assert.Same(category, await repository.GetByIdAsync(category.Id));
        Assert.Same(category, Assert.Single(await repository.GetAllAsync()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAllAsync_with_canceled_token_throws(bool hasCategory)
    {
        var repository = new InMemoryPledgeCategoryRepository();
        if (hasCategory)
        {
            await repository.AddAsync(PledgeCategory.Create("Community Fund"));
        }

        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetAllAsync(tokenSource.Token));

        Assert.Equal(tokenSource.Token, exception.CancellationToken);
    }

    [Fact]
    public async Task GetAllAsync_with_uncanceled_token_returns_categories()
    {
        var repository = new InMemoryPledgeCategoryRepository();
        var category = PledgeCategory.Create("Community Fund");
        await repository.AddAsync(category);
        using var tokenSource = new CancellationTokenSource();

        var result = await repository.GetAllAsync(tokenSource.Token);

        Assert.Same(category, Assert.Single(result));
    }
}
