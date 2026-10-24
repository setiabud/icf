using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.Domain.Repositories;

public interface IPledgeCategoryRepository
{
    Task AddAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken = default);

    Task<PledgeCategory?> GetByIdAsync(PledgeCategoryId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PledgeCategory>> GetAllAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(PledgeCategory pledger, CancellationToken cancellationToken = default);

    Task RemoveAsync(PledgeCategoryId id, CancellationToken cancellationToken = default);
}
