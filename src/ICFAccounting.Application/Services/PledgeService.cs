using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.Application.Services;

/// <summary>
/// Implements application operations for managing pledges.
/// </summary>
public sealed class PledgeService : IPledgeService
{
    private readonly IPledgeRepository _pledgeRepository;
    private readonly ILogger<PledgeService> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PledgeService"/> class.
    /// </summary>
    /// <param name="pledgeRepository">The repository used to retrieve and persist pledges.</param>
    /// <param name="logger">The logger used to record service operations.</param>
    /// <param name="timeProvider">The time provider used to timestamp pledge updates.</param>
    public PledgeService(
        IPledgeRepository pledgeRepository,
        ILogger<PledgeService> logger,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(pledgeRepository);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _pledgeRepository = pledgeRepository;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task AddAsync(Pledge pledge, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pledge);

        await _pledgeRepository.AddAsync(pledge, cancellationToken);
        _logger.PledgeAdded(pledge.Id);
    }

    /// <inheritdoc />
    public async Task<Pledge?> GetByIdAsync(PledgeId id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.ThrowIfEmptyId(id, "Pledge");

        var pledge = await _pledgeRepository.GetByIdAsync(id, cancellationToken);
        if (pledge is null)
        {
            _logger.PledgeNotFound(id);
            return null;
        }

        _logger.PledgeRetrieved(id);
        return pledge;
    }

    /// <inheritdoc />
    public async Task<PagedPledgesResult> GetByPledgerAsync(
        PledgerId pledgerId, PledgeQuery? query = null, CancellationToken cancellationToken = default)
    {
        ServiceGuard.ThrowIfEmptyId(pledgerId, "Pledger");

        query ??= new PledgeQuery();
        var pledges = await _pledgeRepository.GetByPledgerAsync(pledgerId, query, cancellationToken);
        _logger.PledgesByPledgerRetrieved(pledgerId, pledges.Items.Count);
        return pledges;
    }

    /// <inheritdoc />
    public async Task<PagedPledgesResult> GetByCategoryAsync(
        PledgeCategoryId pledgeCategoryId,
        PledgeQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        ServiceGuard.ThrowIfEmptyId(pledgeCategoryId, "Pledge category");

        query ??= new PledgeQuery();
        var pledges = await _pledgeRepository.GetByCategoryAsync(pledgeCategoryId, query, cancellationToken);
        _logger.PledgesByCategoryRetrieved(pledgeCategoryId, pledges.Items.Count);
        return pledges;
    }

    /// <inheritdoc />
    public async Task<Pledge?> UpdateAsync(
        PledgeId id,
        int year,
        decimal amount,
        PledgerId pledgerId,
        PledgeCategoryId pledgeCategoryId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        ServiceGuard.ThrowIfEmptyId(id, "Pledge");

        var pledge = await _pledgeRepository.GetByIdAsync(id, cancellationToken);
        if (pledge is null)
        {
            _logger.PledgeNotFoundForUpdate(id);
            return null;
        }

        pledge.Update(year, amount, pledgerId, pledgeCategoryId, comment, _timeProvider);
        await _pledgeRepository.UpdateAsync(pledge, cancellationToken);
        _logger.PledgeUpdated(id);
        return pledge;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(PledgeId id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.ThrowIfEmptyId(id, "Pledge");

        await _pledgeRepository.RemoveAsync(id, cancellationToken);
        _logger.PledgeRemoved(id);
    }
}