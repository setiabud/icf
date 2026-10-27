using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ICFAccounting.Data.Repositories;

/// <summary>
/// Persists pledgers in SQL Server and saves each mutation before returning.
/// </summary>
public sealed class SqlServerPledgerRepository : IPledgerRepository
{
    private readonly ICFAccountingDbContext _context;

    /// <summary>Creates a repository using the scoped accounting context.</summary>
    public SqlServerPledgerRepository(ICFAccountingDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Pledger?> GetByIdAsync(PledgerId pledgerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _context.Pledgers.AsNoTracking()
            .SingleOrDefaultAsync(pledger => pledger.Id == pledgerId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Pledger pledger, CancellationToken cancellationToken = default)
    {
        await SaveAsync(pledger, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Pledger pledger, CancellationToken cancellationToken = default)
    {
        await SaveAsync(pledger, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(PledgerId pledgerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var pledger = await _context.Pledgers.FindAsync([pledgerId], cancellationToken)
            ?? throw new KeyNotFoundException($"Pledger {pledgerId.Value} was not found.");
        _context.Pledgers.Remove(pledger);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedPledgersResult> GetAllAsync(
        PledgerQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var matches = _context.Pledgers.AsNoTracking();
        if (query is null)
        {
            var allPledgers = await matches.ToListAsync(cancellationToken);
            return PagedPledgersResult.Create(allPledgers, 1, Math.Max(1, allPledgers.Count), allPledgers.Count);
        }

        var totalCount = await matches.CountAsync(cancellationToken);
        var offset = ((long)query.Page - 1) * query.PageSize;
        if (offset >= totalCount)
        {
            return PagedPledgersResult.Create([], query.Page, query.PageSize, totalCount);
        }

        var ordered = query.SortOrder == SortOrder.Descending
            ? matches.OrderByDescending(pledger => pledger.PledgerName)
            : matches.OrderBy(pledger => pledger.PledgerName);
        var pledgers = await ordered.ThenBy(pledger => pledger.Id)
            .Skip((int)offset).Take(query.PageSize).ToListAsync(cancellationToken);
        return PagedPledgersResult.Create(pledgers, query.Page, query.PageSize, totalCount);
    }

    private async Task SaveAsync(Pledger pledger, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pledger);
        cancellationToken.ThrowIfCancellationRequested();
        var existing = await _context.Pledgers.FindAsync([pledger.Id], cancellationToken);
        if (existing is null)
        {
            await _context.Pledgers.AddAsync(pledger, cancellationToken);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(pledger);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}