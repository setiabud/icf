using ICFAccounting.Domain.Repositories;
using ICFAccounting.Domain.Pledgers;

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

    public Task<IReadOnlyList<Pledger>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Pledger> pledgers = _pledgers.Values.ToList();
        return Task.FromResult(pledgers);
    }

    public Task UpdateAsync(Pledger pledger, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _pledgers[pledger.Id] = pledger;
        return Task.CompletedTask;
    }
}
