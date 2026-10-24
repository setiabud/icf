using ICFAccounting.Domain.Repositories;
using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.Data.Repositories;

public sealed class InMemoryPledgeCategoryRepository : IPledgeCategoryRepository
{
    private readonly Dictionary<PledgeCategoryId, PledgeCategory> _pledgeCategories = [];

    public Task<PledgeCategory?> GetByIdAsync(PledgeCategoryId pledgeCategoryId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _pledgeCategories.TryGetValue(pledgeCategoryId, out var pledgeCategory);
        return Task.FromResult(pledgeCategory);
    }

    public Task AddAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _pledgeCategories[pledgeCategory.Id] = pledgeCategory;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(PledgeCategoryId pledgeCategoryId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_pledgeCategories.Remove(pledgeCategoryId))
        {
            throw new KeyNotFoundException($"PledgeCategory {pledgeCategoryId.Value} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _pledgeCategories[pledgeCategory.Id] = pledgeCategory;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PledgeCategory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PledgeCategory>>([.. _pledgeCategories.Values]);
    }
}
