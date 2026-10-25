using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.Data.Repositories;

/// <summary>
/// Stores pledge payments in memory for the lifetime of this repository instance.
/// </summary>
public sealed class InMemoryPledgePaymentRepository : IPledgePaymentRepository
{
    private readonly Dictionary<PledgePaymentId, PledgePayment> _pledgePayments = [];
    private readonly ILogger<InMemoryPledgePaymentRepository> _logger;

    public InMemoryPledgePaymentRepository(ILogger<InMemoryPledgePaymentRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AddAsync(
        PledgePayment pledgePayment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pledgePayment, nameof(pledgePayment));
        cancellationToken.ThrowIfCancellationRequested();

        _pledgePayments[pledgePayment.Id] = pledgePayment;
        _logger.LogDebug("Added pledge payment {PledgePaymentId}.", pledgePayment.Id);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PledgePayment?> GetByIdAsync(
        PledgePaymentId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _pledgePayments.TryGetValue(id, out var pledgePayment);
        _logger.LogDebug("Retrieved pledge payment {PledgePaymentId}.", id);
        await Task.CompletedTask.ConfigureAwait(false);
        return pledgePayment;
    }

    /// <inheritdoc />
    public async Task<PagedPledgePaymentsResult> GetByPledgeAsync(
        PledgeId pledgeId,
        PledgePaymentQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payments = _pledgePayments.Values
            .Where(payment => payment.PledgeId == pledgeId)
            .ToList();
        var result = CreatePage(payments, query ?? new PledgePaymentQuery());
        _logger.LogDebug(
            "Retrieved {PledgePaymentCount} payments for pledge {PledgeId}.",
            result.Items.Count,
            pledgeId);
        await Task.CompletedTask.ConfigureAwait(false);
        return result;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        PledgePayment pledgePayment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pledgePayment, nameof(pledgePayment));
        cancellationToken.ThrowIfCancellationRequested();

        _pledgePayments[pledgePayment.Id] = pledgePayment;
        _logger.LogDebug("Updated pledge payment {PledgePaymentId}.", pledgePayment.Id);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(PledgePaymentId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_pledgePayments.Remove(id))
        {
            throw new KeyNotFoundException($"Pledge payment {id.Value} was not found.");
        }

        _logger.LogDebug("Removed pledge payment {PledgePaymentId}.", id);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static PagedPledgePaymentsResult CreatePage(
        IReadOnlyList<PledgePayment> matches, PledgePaymentQuery query)
    {
        var totalCount = matches.Count;
        var offset = ((long)query.Page - 1) * query.PageSize;
        if (offset >= totalCount)
        {
            return PagedPledgePaymentsResult.Create([], query.Page, query.PageSize, totalCount);
        }

        var orderedPayments = query.SortBy switch
        {
            PledgePaymentSortBy.PaymentDate => query.SortOrder == SortOrder.Descending
                ? matches.OrderByDescending(payment => payment.PaymentDate)
                : matches.OrderBy(payment => payment.PaymentDate),
            PledgePaymentSortBy.Amount => query.SortOrder == SortOrder.Descending
                ? matches.OrderByDescending(payment => payment.Amount)
                : matches.OrderBy(payment => payment.Amount),
            _ => throw new ArgumentOutOfRangeException(nameof(query), "Unsupported pledge payment sort field.")
        };

        IReadOnlyList<PledgePayment> payments = [.. orderedPayments
            .ThenByDescending(payment => payment.Id.Value)
            .Skip((int)offset)
            .Take(query.PageSize)];
        return PagedPledgePaymentsResult.Create(payments, query.Page, query.PageSize, totalCount);
    }
}