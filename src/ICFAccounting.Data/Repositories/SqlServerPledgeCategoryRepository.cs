using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ICFAccounting.Data.Repositories;

/// <summary>
/// Persists pledge categories in SQL Server and saves each mutation before returning.
/// </summary>
public sealed class SqlServerPledgeCategoryRepository : IPledgeCategoryRepository
{
    private readonly ICFAccountingDbContext _context;

    /// <summary>Creates a repository using the scoped accounting context.</summary>
    public SqlServerPledgeCategoryRepository(ICFAccountingDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task<PledgeCategory?> GetByIdAsync(
        PledgeCategoryId pledgeCategoryId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _context.PledgeCategories.AsNoTracking()
            .SingleOrDefaultAsync(category => category.Id == pledgeCategoryId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PledgeCategory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _context.PledgeCategories.AsNoTracking().ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken = default)
    {
        await SaveAsync(pledgeCategory, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken = default)
    {
        await SaveAsync(pledgeCategory, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(PledgeCategoryId pledgeCategoryId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var category = await _context.PledgeCategories.FindAsync([pledgeCategoryId], cancellationToken)
            ?? throw new KeyNotFoundException($"PledgeCategory {pledgeCategoryId.Value} was not found.");
        _context.PledgeCategories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pledgeCategory);
        cancellationToken.ThrowIfCancellationRequested();
        var existing = await _context.PledgeCategories.FindAsync([pledgeCategory.Id], cancellationToken);
        if (existing is null)
        {
            await _context.PledgeCategories.AddAsync(pledgeCategory, cancellationToken);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(pledgeCategory);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}