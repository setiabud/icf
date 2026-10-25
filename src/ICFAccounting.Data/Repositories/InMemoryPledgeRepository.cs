using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;

namespace ICFAccounting.Data.Repositories;

/// <summary>
/// Stores pledges in memory for the lifetime of this repository instance.
/// </summary>
public sealed class InMemoryPledgeRepository : IPledgeRepository
{
    private readonly Dictionary<PledgeId, Pledge> _pledges = [];

    /// <inheritdoc />
    public Task AddAsync(Pledge pledge, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pledge, nameof(pledge));
        cancellationToken.ThrowIfCancellationRequested();

        _pledges[pledge.Id] = pledge;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Pledge?> GetByIdAsync(PledgeId pledgeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _pledges.TryGetValue(pledgeId, out var pledge);
        return Task.FromResult(pledge);
    }

    /// <inheritdoc />
    public Task<PagedPledgesResult> GetByPledgerAsync(
        PledgerId pledgerId,
        PledgeQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var matches = _pledges.Values.Where(pledge => pledge.PledgerId == pledgerId).ToList();
        return Task.FromResult(CreatePage(matches, query ?? new PledgeQuery()));
    }

    /// <inheritdoc />
    public Task<PagedPledgesResult> GetByCategoryAsync(
        PledgeCategoryId pledgeCategoryId,
        PledgeQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var matches = _pledges.Values.Where(pledge => pledge.PledgeCategoryId == pledgeCategoryId).ToList();
        return Task.FromResult(CreatePage(matches, query ?? new PledgeQuery()));
    }

    /// <inheritdoc />
    public Task UpdateAsync(Pledge pledge, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pledge, nameof(pledge));
        cancellationToken.ThrowIfCancellationRequested();

        _pledges[pledge.Id] = pledge;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(PledgeId pledgeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_pledges.Remove(pledgeId))
        {
            throw new KeyNotFoundException($"Pledge {pledgeId.Value} was not found.");
        }

        return Task.CompletedTask;
    }

    private static PagedPledgesResult CreatePage(IReadOnlyList<Pledge> matches, PledgeQuery query)
    {
        var totalCount = matches.Count;
        var offset = ((long)query.Page - 1) * query.PageSize;
        if (offset >= totalCount)
        {
            return PagedPledgesResult.Create([], query.Page, query.PageSize, totalCount);
        }

        var orderedPledges = query.SortBy == PledgeSortBy.Amount
            ? query.SortOrder == SortOrder.Descending
                ? matches.OrderByDescending(pledge => pledge.Amount)
                : matches.OrderBy(pledge => pledge.Amount)
            : query.SortOrder == SortOrder.Descending
                ? matches.OrderByDescending(pledge => pledge.Year)
                : matches.OrderBy(pledge => pledge.Year);

        IReadOnlyList<Pledge> pledges = [.. orderedPledges
            .ThenByDescending(pledge => pledge.Id.Value)
            .Skip((int)offset)
            .Take(query.PageSize)];
        return PagedPledgesResult.Create(pledges, query.Page, query.PageSize, totalCount);
    }
}
