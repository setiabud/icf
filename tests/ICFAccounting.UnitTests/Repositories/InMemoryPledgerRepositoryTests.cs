using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.UnitTests.Repositories;

public sealed class InMemoryPledgerRepositoryTests
{
    [Fact]
    public async Task CountAsync_returns_total_independent_of_pagination_and_tracks_removals()
    {
        var repository = new InMemoryPledgerRepository();
        Assert.Equal(0, await repository.CountAsync());
        var first = Pledger.Create("Alpha");
        await repository.AddAsync(first);
        await repository.AddAsync(Pledger.Create("Bravo"));
        await repository.AddAsync(first);

        await repository.GetAllAsync(CancellationToken.None, new PledgerQuery(1, 1));

        Assert.Equal(2, await repository.CountAsync());
        await repository.RemoveAsync(first.Id);
        Assert.Equal(1, await repository.CountAsync());
    }

    [Fact]
    public async Task CountAsync_with_canceled_token_throws()
    {
        var repository = new InMemoryPledgerRepository();
        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.CountAsync(tokenSource.Token));
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
            CancellationToken.None, new PledgerQuery(page, pageSize, sortOrder: sortOrder));

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

        var result = await repository.GetAllAsync(CancellationToken.None, new PledgerQuery());

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

    [Fact]
    public async Task GetAllAsync_with_equal_names_uses_identity_for_stable_pages()
    {
        var repository = new InMemoryPledgerRepository();
        var first = Pledger.Create("Alpha");
        var second = Pledger.Create("alpha");
        await repository.AddAsync(first);
        await repository.AddAsync(second);

        var pageOne = await repository.GetAllAsync(CancellationToken.None, new PledgerQuery(1, 1));
        var pageTwo = await repository.GetAllAsync(CancellationToken.None, new PledgerQuery(2, 1));

        Assert.Equal(new[] { first.Id.Value, second.Id.Value }.Order(),
            pageOne.Items.Concat(pageTwo.Items).Select(pledger => pledger.Id.Value));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAllAsync_with_empty_repository_returns_zero_total(bool paginated)
    {
        var repository = new InMemoryPledgerRepository();

        var result = await repository.GetAllAsync(
            CancellationToken.None, paginated ? new PledgerQuery(3, 5) : null);

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
            () => repository.GetAllAsync(tokenSource.Token, new PledgerQuery()));
    }
}
