using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.Domain.Repositories;

public interface IPledgerRepository
{
    Task AddAsync(Pledger pledger, CancellationToken cancellationToken = default);

    Task<Pledger?> GetByIdAsync(PledgerId id, CancellationToken cancellationToken = default);

    Task<PagedPledgersResult> GetAllAsync(PledgerQuery? query = null, CancellationToken cancellationToken = default);

    Task UpdateAsync(Pledger pledger, CancellationToken cancellationToken = default);

    Task RemoveAsync(PledgerId id, CancellationToken cancellationToken = default);
}
