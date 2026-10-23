using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.Domain.Repositories;

public interface IPledgeCategoryRepository
{
    Task AddAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken);

    Task<PledgeCategory?> GetByIdAsync(PledgeCategoryId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<PledgeCategory>> GetAllAsync(CancellationToken cancellationToken);

    Task UpdateAsync(PledgeCategory pledger, CancellationToken cancellationToken);

    Task RemoveAsync(PledgeCategoryId id, CancellationToken cancellationToken);
}
