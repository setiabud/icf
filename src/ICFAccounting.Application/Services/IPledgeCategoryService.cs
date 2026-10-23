using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.Application.Services;

public interface IPledgeCategoryService
{
    Task<IReadOnlyList<PledgeCategory>> GetAllAsync(CancellationToken cancellationToken);

    Task<PledgeCategory?> GetByIdAsync(PledgeCategoryId id, CancellationToken cancellationToken);

    Task AddAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken);

    Task UpdateAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken);

    Task RemoveAsync(PledgeCategoryId id, CancellationToken cancellationToken);
}
