using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.Application.Services;

/// <summary>
/// Implements application operations for managing pledge payments.
/// </summary>
public sealed class PledgePaymentService : IPledgePaymentService
{
    private readonly IPledgePaymentRepository _pledgePaymentRepository;
    private readonly ILogger<PledgePaymentService> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PledgePaymentService"/> class.
    /// </summary>
    /// <param name="pledgePaymentRepository">The repository used to retrieve and persist payments.</param>
    /// <param name="logger">The logger used to record service operations.</param>
    /// <param name="timeProvider">The time provider used to timestamp payment updates.</param>
    public PledgePaymentService(
        IPledgePaymentRepository pledgePaymentRepository,
        ILogger<PledgePaymentService> logger,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(pledgePaymentRepository);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _pledgePaymentRepository = pledgePaymentRepository;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task AddAsync(
        PledgePayment pledgePayment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pledgePayment);

        await _pledgePaymentRepository.AddAsync(pledgePayment, cancellationToken);
        _logger.PledgePaymentAdded(pledgePayment.Id);
    }

    /// <inheritdoc />
    public async Task<PledgePayment?> GetByIdAsync(
        PledgePaymentId id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.ThrowIfEmptyId(id, "Pledge payment");

        var pledgePayment = await _pledgePaymentRepository.GetByIdAsync(id, cancellationToken);
        if (pledgePayment is null)
        {
            _logger.PledgePaymentNotFound(id);
            return null;
        }

        _logger.PledgePaymentRetrieved(id);
        return pledgePayment;
    }

    /// <inheritdoc />
    public async Task<PagedPledgePaymentsResult> GetByPledgeAsync(
        PledgeId pledgeId,
        PledgePaymentQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        ServiceGuard.ThrowIfEmptyId(pledgeId, "Pledge");

        query ??= new PledgePaymentQuery();
        var payments = await _pledgePaymentRepository.GetByPledgeAsync(pledgeId, query, cancellationToken);
        _logger.PledgePaymentsByPledgeRetrieved(pledgeId, payments.Items.Count);
        return payments;
    }

    /// <inheritdoc />
    public async Task<PledgePayment?> UpdateAsync(
        PledgePaymentId id,
        PledgeId pledgeId,
        decimal amount,
        DateTime paymentDate,
        string? comment,
        bool active,
        CancellationToken cancellationToken = default)
    {
        ServiceGuard.ThrowIfEmptyId(id, "Pledge payment");
        ServiceGuard.ThrowIfEmptyId(pledgeId, "Pledge");

        var pledgePayment = await _pledgePaymentRepository.GetByIdAsync(id, cancellationToken);
        if (pledgePayment is null)
        {
            _logger.PledgePaymentNotFoundForUpdate(id);
            return null;
        }

        pledgePayment.Update(pledgeId, amount, paymentDate, comment, active, _timeProvider);
        await _pledgePaymentRepository.UpdateAsync(pledgePayment, cancellationToken);
        _logger.PledgePaymentUpdated(id);
        return pledgePayment;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(PledgePaymentId id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.ThrowIfEmptyId(id, "Pledge payment");

        await _pledgePaymentRepository.RemoveAsync(id, cancellationToken);
        _logger.PledgePaymentRemoved(id);
    }
}