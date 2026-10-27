using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ICFAccounting.Data.Repositories;

/// <summary>
/// Persists pledges in SQL Server with server-side filtering, ordering, and paging.
/// </summary>
public sealed class SqlServerPledgeRepository : IPledgeRepository
{
    private readonly ICFAccountingDbContext _context;

    /// <summary>Creates a repository using the scoped accounting context.</summary>
    public SqlServerPledgeRepository(ICFAccountingDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(Pledge pledge, CancellationToken cancellationToken = default)
    {
        await SaveAsync(pledge, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Pledge pledge, CancellationToken cancellationToken = default)
    {
        await SaveAsync(pledge, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Pledge?> GetByIdAsync(PledgeId pledgeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _context.Pledges.AsNoTracking()
            .SingleOrDefaultAsync(pledge => pledge.Id == pledgeId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedPledgesResult> GetByPledgerAsync(
        PledgerId pledgerId,
        PledgeQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var matches = _context.Pledges.AsNoTracking().Where(pledge => pledge.PledgerId == pledgerId);
        return await CreatePageAsync(matches, query ?? new PledgeQuery(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedPledgesResult> GetByCategoryAsync(
        PledgeCategoryId pledgeCategoryId,
        PledgeQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var matches = _context.Pledges.AsNoTracking().Where(pledge => pledge.PledgeCategoryId == pledgeCategoryId);
        return await CreatePageAsync(matches, query ?? new PledgeQuery(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(PledgeId pledgeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var pledge = await _context.Pledges.FindAsync([pledgeId], cancellationToken)
            ?? throw new KeyNotFoundException($"Pledge {pledgeId.Value} was not found.");
        _context.Pledges.Remove(pledge);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveAsync(Pledge pledge, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pledge);
        cancellationToken.ThrowIfCancellationRequested();
        var existing = await _context.Pledges.FindAsync([pledge.Id], cancellationToken);
        if (existing is null)
        {
            await _context.Pledges.AddAsync(pledge, cancellationToken);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(pledge);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<PagedPledgesResult> CreatePageAsync(
        IQueryable<Pledge> matches, PledgeQuery query, CancellationToken cancellationToken)
    {
        var totalCount = await matches.CountAsync(cancellationToken);
        var offset = ((long)query.Page - 1) * query.PageSize;
        if (offset >= totalCount)
        {
            return PagedPledgesResult.Create([], query.Page, query.PageSize, totalCount);
        }

        var ordered = query.SortBy == PledgeSortBy.Amount
            ? query.SortOrder == SortOrder.Descending
                ? matches.OrderByDescending(pledge => pledge.Amount)
                : matches.OrderBy(pledge => pledge.Amount)
            : query.SortOrder == SortOrder.Descending
                ? matches.OrderByDescending(pledge => pledge.Year)
                : matches.OrderBy(pledge => pledge.Year);
        var pledges = await ordered.ThenByDescending(pledge => pledge.Id)
            .Skip((int)offset).Take(query.PageSize).ToListAsync(cancellationToken);
        return PagedPledgesResult.Create(pledges, query.Page, query.PageSize, totalCount);
    }
}