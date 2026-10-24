using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.Domain.Repositories;

/// <summary>
/// Persistence port for <see cref="Pledge"/> entities.
/// </summary>
public interface IPledgeRepository
{
    Task AddAsync(Pledge pledge, CancellationToken cancellationToken = default);

    Task<Pledge?> GetByIdAsync(PledgeId id, CancellationToken cancellationToken = default);

    Task<PagedPledgesResult> GetByPledgerAsync(
        PledgerId pledgerId, PledgeQuery? query = null, CancellationToken cancellationToken = default);

    Task<PagedPledgesResult> GetByCategoryAsync(
        PledgeCategoryId pledgeCategoryId, PledgeQuery? query = null, CancellationToken cancellationToken = default);

    Task UpdateAsync(Pledge pledge, CancellationToken cancellationToken = default);

    Task RemoveAsync(PledgeId id, CancellationToken cancellationToken = default);
}
