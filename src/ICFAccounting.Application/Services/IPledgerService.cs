using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.Application.Services;

public interface IPledgerService
{
    Task AddAsync(Pledger pledger, CancellationToken cancellationToken);

    Task<Pledger?> GetByIdAsync(PledgerId id, CancellationToken cancellationToken);

    Task<PagedPledgersResult> GetAllAsync(CancellationToken cancellationToken, PledgerQuery? query = null);

    Task<Pledger?> UpdateAsync(
        PledgerId id, string pledgerName, bool active, CancellationToken cancellationToken);

    Task RemoveAsync(PledgerId id, CancellationToken cancellationToken);
}
