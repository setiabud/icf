using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.Domain.Repositories;

public interface IPledgerRepository
{
    Task AddAsync(Pledger pledger, CancellationToken cancellationToken);

    Task<Pledger?> GetByIdAsync(PledgerId id, CancellationToken cancellationToken);

    Task<PagedPledgersResult> GetAllAsync(CancellationToken cancellationToken, PledgerQuery? query = null);

    Task<int> CountAsync(CancellationToken cancellationToken);

    Task UpdateAsync(Pledger pledger, CancellationToken cancellationToken);

    Task RemoveAsync(PledgerId id, CancellationToken cancellationToken);
}
