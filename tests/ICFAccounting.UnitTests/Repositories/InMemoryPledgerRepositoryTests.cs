using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.UnitTests.Repositories;

public sealed class InMemoryPledgerRepositoryTests
{
    [Fact]
    public async Task AddAsync_then_GetByIdAsync_returns_pledger()
    {
        var repository = new InMemoryPledgerRepository();
        var pledger = Pledger.Create("Community Fund");

        await repository.AddAsync(pledger);

        Assert.Same(pledger, await repository.GetByIdAsync(pledger.Id));
    }

    [Fact]
    public async Task GetByIdAsync_with_unknown_id_returns_null()
    {
        var repository = new InMemoryPledgerRepository();
        await repository.AddAsync(Pledger.Create("Community Fund"));

        Assert.Null(await repository.GetByIdAsync(PledgerId.New()));
    }

    [Fact]
    public async Task UpdateAsync_with_unknown_pledger_adds_it()
    {
        var repository = new InMemoryPledgerRepository();
        var pledger = Pledger.Create("Community Fund");

        await repository.UpdateAsync(pledger);

        Assert.Same(pledger, await repository.GetByIdAsync(pledger.Id));
    }

    [Fact]
    public async Task RemoveAsync_deletes_only_requested_pledger()
    {
        var repository = new InMemoryPledgerRepository();
        var pledger = Pledger.Create("Community Fund");
        var otherPledger = Pledger.Create("Building Fund");
        await repository.AddAsync(pledger);
        await repository.AddAsync(otherPledger);

        await repository.RemoveAsync(pledger.Id);

        Assert.Null(await repository.GetByIdAsync(pledger.Id));
        Assert.Same(otherPledger, Assert.Single((await repository.GetAllAsync()).Items));
    }

    [Fact]
    public async Task RemoveAsync_with_unknown_id_throws_without_removing_pledgers()
    {
        var repository = new InMemoryPledgerRepository();
        var pledger = Pledger.Create("Community Fund");
        await repository.AddAsync(pledger);
        var unknownId = PledgerId.New();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => repository.RemoveAsync(unknownId));

        Assert.Equal($"Pledger {unknownId.Value} was not found.", exception.Message);
        Assert.Same(pledger, Assert.Single((await repository.GetAllAsync()).Items));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAllAsync_returns_snapshot_unaffected_by_later_additions_and_removals(bool paginated)
    {
        var repository = new InMemoryPledgerRepository();
        var pledger = Pledger.Create("Community Fund");
        var otherPledger = Pledger.Create("Building Fund");
        await repository.AddAsync(pledger);

        var snapshot = await repository.GetAllAsync(paginated ? new PledgerQuery() : null);
        await repository.AddAsync(otherPledger);

        Assert.Same(pledger, Assert.Single(snapshot.Items));
        Assert.Equal(1, snapshot.TotalCount);

        await repository.RemoveAsync(pledger.Id);

        Assert.Same(pledger, Assert.Single(snapshot.Items));
        Assert.Equal(1, snapshot.TotalCount);
        Assert.Same(otherPledger, Assert.Single((await repository.GetAllAsync()).Items));
    }

    [Fact]
    public async Task AddAsync_with_canceled_token_throws_without_adding_pledger()
    {
        var repository = new InMemoryPledgerRepository();
        var pledger = Pledger.Create("Community Fund");
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.AddAsync(pledger, tokenSource.Token));

        Assert.Equal(tokenSource.Token, exception.CancellationToken);
        Assert.Null(await repository.GetByIdAsync(pledger.Id));
        Assert.Empty((await repository.GetAllAsync()).Items);
    }

    [Fact]
    public async Task GetByIdAsync_with_canceled_token_throws()
    {
        var repository = new InMemoryPledgerRepository();
        var pledger = Pledger.Create("Community Fund");
        await repository.AddAsync(pledger);
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetByIdAsync(pledger.Id, tokenSource.Token));

        Assert.Equal(tokenSource.Token, exception.CancellationToken);
    }

    [Fact]
    public async Task UpdateAsync_with_canceled_token_throws_without_inserting_pledger()
    {
        var repository = new InMemoryPledgerRepository();
        var pledger = Pledger.Create("Community Fund");
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.UpdateAsync(pledger, tokenSource.Token));

        Assert.Equal(tokenSource.Token, exception.CancellationToken);
        Assert.Null(await repository.GetByIdAsync(pledger.Id));
        Assert.Empty((await repository.GetAllAsync()).Items);
    }

    [Theory]
    [InlineData(1, 2, SortOrder.Ascending, "Alpha,bravo")]
    [InlineData(2, 2, SortOrder.Ascending, "Charlie,delta")]
    [InlineData(3, 2, SortOrder.Ascending, "Echo")]
    [InlineData(4, 2, SortOrder.Ascending, "")]
    [InlineData(1, 2, SortOrder.Descending, "Echo,delta")]
    [InlineData(2, 2, SortOrder.Descending, "Charlie,bravo")]
    [InlineData(int.MaxValue, 100, SortOrder.Ascending, "")]
    public async Task GetAllAsync_sorts_before_selecting_page(
        int page, int pageSize, SortOrder sortOrder, string expectedNames)
    {
        var repository = new InMemoryPledgerRepository();
        foreach (var name in new[] { "Charlie", "Echo", "Alpha", "delta", "bravo" })
        {
            await repository.AddAsync(Pledger.Create(name));
        }

        var result = await repository.GetAllAsync(
            new PledgerQuery(page, pageSize, sortOrder: sortOrder), CancellationToken.None);

        Assert.Equal(expectedNames, string.Join(",", result.Items.Select(pledger => pledger.PledgerName)));
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(page, result.Page);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal((int)Math.Ceiling(5.0 / pageSize), result.TotalPages);
    }

    [Fact]
    public async Task GetAllAsync_with_default_query_returns_twenty_sorted_entries()
    {
        var repository = new InMemoryPledgerRepository();
        foreach (var number in Enumerable.Range(1, 22).Reverse())
        {
            await repository.AddAsync(Pledger.Create($"Pledger {number:D2}"));
        }

        var result = await repository.GetAllAsync(new PledgerQuery(), CancellationToken.None);

        Assert.Equal(Enumerable.Range(1, 20).Select(number => $"Pledger {number:D2}"),
            result.Items.Select(pledger => pledger.PledgerName));
        Assert.Equal(22, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        var unpaged = await repository.GetAllAsync();
        Assert.Equal(22, unpaged.Items.Count);
        Assert.Equal(22, unpaged.TotalCount);
        Assert.Equal(1, unpaged.Page);
        Assert.Equal(22, unpaged.PageSize);
        Assert.Equal(1, unpaged.TotalPages);
    }

    [Theory]
    [InlineData(SortOrder.Ascending)]
    [InlineData(SortOrder.Descending)]
    public async Task GetAllAsync_with_equal_names_uses_identity_for_stable_pages(SortOrder sortOrder)
    {
        var repository = new InMemoryPledgerRepository();
        var pledgers = new[] { Pledger.Create("Alpha"), Pledger.Create("alpha") }
            .OrderBy(pledger => pledger.Id.Value)
            .ToArray();
        foreach (var pledger in pledgers.Reverse())
        {
            await repository.AddAsync(pledger);
        }

        var pageOne = await repository.GetAllAsync(
            new PledgerQuery(1, 1, sortOrder: sortOrder), CancellationToken.None);
        var pageTwo = await repository.GetAllAsync(
            new PledgerQuery(2, 1, sortOrder: sortOrder), CancellationToken.None);

        Assert.Equal(pledgers.Select(pledger => pledger.Id.Value),
            pageOne.Items.Concat(pageTwo.Items).Select(pledger => pledger.Id.Value));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAllAsync_with_empty_repository_returns_zero_total(bool paginated)
    {
        var repository = new InMemoryPledgerRepository();

        var result = await repository.GetAllAsync(
            paginated ? new PledgerQuery(3, 5) : null, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.Equal(paginated ? 3 : 1, result.Page);
        Assert.Equal(paginated ? 5 : 1, result.PageSize);
    }

    [Fact]
    public async Task GetAllAsync_with_canceled_token_throws()
    {
        var repository = new InMemoryPledgerRepository();
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetAllAsync(new PledgerQuery(), tokenSource.Token));
    }

    [Fact]
    public async Task RemoveAsync_with_canceled_token_throws_without_removing_pledger()
    {
        var repository = new InMemoryPledgerRepository();
        var pledger = Pledger.Create("Community Fund");
        await repository.AddAsync(pledger);
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.RemoveAsync(pledger.Id, tokenSource.Token));

        Assert.Same(pledger, await repository.GetByIdAsync(pledger.Id));
    }
}
