using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.UnitTests.Repositories;

public sealed class InMemoryPledgeRepositoryTests
{
    private static readonly PledgerId PledgerA = PledgerId.New();
    private static readonly PledgerId PledgerB = PledgerId.New();
    private static readonly PledgeCategoryId CategoryA = PledgeCategoryId.New();
    private static readonly PledgeCategoryId CategoryB = PledgeCategoryId.New();

    [Fact]
    public async Task AddAsync_then_GetByIdAsync_returns_pledge()
    {
        var repository = new InMemoryPledgeRepository();
        var pledge = CreatePledge(2025, PledgerA, CategoryA);

        await repository.AddAsync(pledge, CancellationToken.None);

        Assert.Same(pledge, await repository.GetByIdAsync(pledge.Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetByIdAsync_with_unknown_id_returns_null()
    {
        var repository = new InMemoryPledgeRepository();

        Assert.Null(await repository.GetByIdAsync(PledgeId.New(), CancellationToken.None));
    }

    [Fact]
    public async Task AddAsync_with_null_pledge_throws()
    {
        var repository = new InMemoryPledgeRepository();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.AddAsync(null!, CancellationToken.None));
        Assert.Equal("pledge", exception.ParamName);
    }

    [Fact]
    public async Task UpdateAsync_replaces_stored_pledge()
    {
        var repository = new InMemoryPledgeRepository();
        var pledge = CreatePledge(2025, PledgerA, CategoryA);
        await repository.AddAsync(pledge, CancellationToken.None);

        pledge.Update(2026, 250m, PledgerB, CategoryB, "Updated");
        await repository.UpdateAsync(pledge, CancellationToken.None);

        var stored = await repository.GetByIdAsync(pledge.Id, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal(2026, stored.Year);
        Assert.Equal(PledgerB, stored.PledgerId);
    }

    [Fact]
    public async Task UpdateAsync_with_unknown_pledge_adds_it()
    {
        var repository = new InMemoryPledgeRepository();
        var pledge = CreatePledge(2025, PledgerA, CategoryA);

        await repository.UpdateAsync(pledge, CancellationToken.None);

        Assert.Same(pledge, await repository.GetByIdAsync(pledge.Id, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_with_null_pledge_throws()
    {
        var repository = new InMemoryPledgeRepository();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.UpdateAsync(null!, CancellationToken.None));
        Assert.Equal("pledge", exception.ParamName);
    }

    [Fact]
    public async Task RemoveAsync_deletes_pledge()
    {
        var repository = new InMemoryPledgeRepository();
        var pledge = CreatePledge(2025, PledgerA, CategoryA);
        await repository.AddAsync(pledge, CancellationToken.None);

        await repository.RemoveAsync(pledge.Id, CancellationToken.None);

        Assert.Null(await repository.GetByIdAsync(pledge.Id, CancellationToken.None));
    }

    [Fact]
    public async Task RemoveAsync_with_unknown_id_throws_key_not_found()
    {
        var repository = new InMemoryPledgeRepository();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => repository.RemoveAsync(PledgeId.New(), CancellationToken.None));
    }

    [Theory]
    [InlineData(1, 2, SortOrder.Ascending, "2021,2022")]
    [InlineData(2, 2, SortOrder.Ascending, "2023,2024")]
    [InlineData(3, 2, SortOrder.Ascending, "2025")]
    [InlineData(4, 2, SortOrder.Ascending, "")]
    [InlineData(1, 2, SortOrder.Descending, "2025,2024")]
    [InlineData(int.MaxValue, 100, SortOrder.Ascending, "")]
    public async Task GetByPledgerAsync_filters_sorts_and_pages(
        int page, int pageSize, SortOrder sortOrder, string expectedYears)
    {
        var repository = await CreateSeededRepositoryAsync();

        var result = await repository.GetByPledgerAsync(
            PledgerA, new PledgeQuery(page, pageSize, sortOrder: sortOrder), CancellationToken.None);

        Assert.Equal(expectedYears, string.Join(",", result.Items.Select(pledge => pledge.Year)));
        Assert.All(result.Items, pledge => Assert.Equal(PledgerA, pledge.PledgerId));
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(page, result.Page);
        Assert.Equal(pageSize, result.PageSize);
    }

    [Theory]
    [InlineData(1, 2, SortOrder.Ascending, "2021,2023")]
    [InlineData(2, 2, SortOrder.Ascending, "2025")]
    [InlineData(1, 10, SortOrder.Descending, "2025,2023,2021")]
    public async Task GetByCategoryAsync_filters_sorts_and_pages(
        int page, int pageSize, SortOrder sortOrder, string expectedYears)
    {
        var repository = await CreateSeededRepositoryAsync();

        var result = await repository.GetByCategoryAsync(
            CategoryA, new PledgeQuery(page, pageSize, sortOrder: sortOrder), CancellationToken.None);

        Assert.Equal(expectedYears, string.Join(",", result.Items.Select(pledge => pledge.Year)));
        Assert.All(result.Items, pledge => Assert.Equal(CategoryA, pledge.PledgeCategoryId));
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task GetByPledgerAsync_without_query_applies_default_pagination()
    {
        var repository = new InMemoryPledgeRepository();
        foreach (var year in Enumerable.Range(2030, 22))
        {
            await repository.AddAsync(CreatePledge(year, PledgerA, CategoryA), CancellationToken.None);
        }

        var result = await repository.GetByPledgerAsync(PledgerA);

        Assert.Equal(Enumerable.Range(2032, 20).Reverse(), result.Items.Select(pledge => pledge.Year));
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(22, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetByCategoryAsync_without_query_applies_default_pagination()
    {
        var repository = await CreateSeededRepositoryAsync();

        var result = await repository.GetByCategoryAsync(CategoryB);

        Assert.Equal("2030,2024,2022", string.Join(",", result.Items.Select(pledge => pledge.Year)));
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task GetByPledgerAsync_with_equal_years_uses_identity_for_stable_pages()
    {
        var repository = new InMemoryPledgeRepository();
        var first = CreatePledge(2025, PledgerA, CategoryA);
        var second = CreatePledge(2025, PledgerA, CategoryA);
        await repository.AddAsync(first, CancellationToken.None);
        await repository.AddAsync(second, CancellationToken.None);

        var pageOne = await repository.GetByPledgerAsync(PledgerA, new PledgeQuery(1, 1), CancellationToken.None);
        var pageTwo = await repository.GetByPledgerAsync(PledgerA, new PledgeQuery(2, 1), CancellationToken.None);

        Assert.Equal(new[] { first.Id.Value, second.Id.Value }.Order(),
            pageOne.Items.Concat(pageTwo.Items).Select(pledge => pledge.Id.Value));
    }

    [Fact]
    public async Task GetByPledgerAsync_with_no_matches_returns_empty_page()
    {
        var repository = await CreateSeededRepositoryAsync();

        var result = await repository.GetByPledgerAsync(PledgerId.New());

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task Methods_with_canceled_token_throw()
    {
        var repository = new InMemoryPledgeRepository();
        var pledge = CreatePledge(2025, PledgerA, CategoryA);
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();
        var token = tokenSource.Token;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.AddAsync(pledge, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetByIdAsync(pledge.Id, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.UpdateAsync(pledge, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.RemoveAsync(pledge.Id, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByPledgerAsync(PledgerA, cancellationToken: token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByCategoryAsync(CategoryA, cancellationToken: token));
    }

    private static async Task<InMemoryPledgeRepository> CreateSeededRepositoryAsync()
    {
        var repository = new InMemoryPledgeRepository();
        foreach (var year in new[] { 2023, 2025, 2021, 2024, 2022 })
        {
            var category = year % 2 == 1 ? CategoryA : CategoryB;
            await repository.AddAsync(CreatePledge(year, PledgerA, category), CancellationToken.None);
        }

        await repository.AddAsync(CreatePledge(2030, PledgerB, CategoryB), CancellationToken.None);
        return repository;
    }

    private static Pledge CreatePledge(int year, PledgerId pledgerId, PledgeCategoryId categoryId) =>
        Pledge.Create(year, 100m, pledgerId, categoryId);
}
