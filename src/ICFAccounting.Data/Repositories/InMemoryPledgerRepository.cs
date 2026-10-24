using ICFAccounting.Domain.Repositories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Common;

namespace ICFAccounting.Data.Repositories;

public sealed class InMemoryPledgerRepository : IPledgerRepository
{
    private readonly Dictionary<PledgerId, Pledger> _pledgers = [];

    public Task<Pledger?> GetByIdAsync(PledgerId pledgerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _pledgers.TryGetValue(pledgerId, out var pledger);
        return Task.FromResult(pledger);
    }

    public Task AddAsync(Pledger pledger, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _pledgers[pledger.Id] = pledger;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(PledgerId pledgerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_pledgers.Remove(pledgerId))
        {
            throw new KeyNotFoundException($"Pledger {pledgerId.Value} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task<PagedPledgersResult> GetAllAsync(
        PledgerQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = _pledgers.Values.ToList();
        var totalCount = snapshot.Count;

        if (query is null)
        {
            return Task.FromResult(
                PagedPledgersResult.Create(snapshot, 1, Math.Max(1, totalCount), totalCount));
        }

        var offset = ((long)query.Page - 1) * query.PageSize;
        if (offset >= totalCount)
        {
            return Task.FromResult(PagedPledgersResult.Create([], query.Page, query.PageSize, totalCount));
        }

        var orderedPledgers = query.SortOrder == SortOrder.Descending
            ? snapshot.OrderByDescending(pledger => pledger.PledgerName, StringComparer.OrdinalIgnoreCase)
            : snapshot.OrderBy(pledger => pledger.PledgerName, StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<Pledger> pledgers = [.. orderedPledgers
            .ThenBy(pledger => pledger.Id.Value)
            .Skip((int)offset)
            .Take(query.PageSize)];
        return Task.FromResult(PagedPledgersResult.Create(pledgers, query.Page, query.PageSize, totalCount));
    }

    public Task UpdateAsync(Pledger pledger, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _pledgers[pledger.Id] = pledger;
        return Task.CompletedTask;
    }
}
