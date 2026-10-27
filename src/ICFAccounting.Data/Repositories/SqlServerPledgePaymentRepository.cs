using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.Data.Repositories;

/// <summary>
/// Persists pledge payments in SQL Server with server-side ordering and paging.
/// </summary>
public sealed class SqlServerPledgePaymentRepository : IPledgePaymentRepository
{
    private readonly ICFAccountingDbContext _context;
    private readonly ILogger<SqlServerPledgePaymentRepository> _logger;

    /// <summary>Creates a repository using the scoped accounting context and logger.</summary>
    public SqlServerPledgePaymentRepository(
        ICFAccountingDbContext context, ILogger<SqlServerPledgePaymentRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AddAsync(PledgePayment pledgePayment, CancellationToken cancellationToken = default)
    {
        await SaveAsync(pledgePayment, cancellationToken);
        _logger.LogDebug("Added pledge payment {PledgePaymentId}.", pledgePayment.Id);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(PledgePayment pledgePayment, CancellationToken cancellationToken = default)
    {
        await SaveAsync(pledgePayment, cancellationToken);
        _logger.LogDebug("Updated pledge payment {PledgePaymentId}.", pledgePayment.Id);
    }

    /// <inheritdoc />
    public async Task<PledgePayment?> GetByIdAsync(PledgePaymentId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payment = await _context.PledgePayments.AsNoTracking()
            .SingleOrDefaultAsync(payment => payment.Id == id, cancellationToken);
        _logger.LogDebug("Retrieved pledge payment {PledgePaymentId}.", id);
        return payment;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(PledgePaymentId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payment = await _context.PledgePayments.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException($"Pledge payment {id.Value} was not found.");
        _context.PledgePayments.Remove(payment);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Removed pledge payment {PledgePaymentId}.", id);
    }

    /// <inheritdoc />
    public async Task<PagedPledgePaymentsResult> GetByPledgeAsync(
        PledgeId pledgeId,
        PledgePaymentQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        query ??= new PledgePaymentQuery();
        var matches = _context.PledgePayments.AsNoTracking().Where(payment => payment.PledgeId == pledgeId);
        var totalCount = await matches.CountAsync(cancellationToken);
        var offset = ((long)query.Page - 1) * query.PageSize;
        if (offset >= totalCount)
        {
            return PagedPledgePaymentsResult.Create([], query.Page, query.PageSize, totalCount);
        }

        var ordered = query.SortBy switch
        {
            PledgePaymentSortBy.PaymentDate => query.SortOrder == SortOrder.Descending
                ? matches.OrderByDescending(payment => payment.PaymentDate)
                : matches.OrderBy(payment => payment.PaymentDate),
            PledgePaymentSortBy.Amount => query.SortOrder == SortOrder.Descending
                ? matches.OrderByDescending(payment => payment.Amount)
                : matches.OrderBy(payment => payment.Amount),
            _ => throw new ArgumentOutOfRangeException(nameof(query), "Unsupported pledge payment sort field.")
        };
        var payments = await ordered.ThenByDescending(payment => payment.Id)
            .Skip((int)offset).Take(query.PageSize).ToListAsync(cancellationToken);
        _logger.LogDebug("Retrieved {PledgePaymentCount} payments for pledge {PledgeId}.", payments.Count, pledgeId);
        return PagedPledgePaymentsResult.Create(payments, query.Page, query.PageSize, totalCount);
    }

    private async Task SaveAsync(PledgePayment pledgePayment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pledgePayment);
        cancellationToken.ThrowIfCancellationRequested();
        var existing = await _context.PledgePayments.FindAsync([pledgePayment.Id], cancellationToken);
        if (existing is null)
        {
            await _context.PledgePayments.AddAsync(pledgePayment, cancellationToken);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(pledgePayment);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}