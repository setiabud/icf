using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.Application.Services;

/// <summary>
/// Implements application operations for managing pledgers.
/// </summary>
public sealed class PledgerService : IPledgerService
{
    private readonly IPledgerRepository _pledgerRepository;
    private readonly ILogger<PledgerService> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PledgerService"/> class.
    /// </summary>
    /// <param name="pledgerRepository">The repository used to retrieve and persist pledgers.</param>
    /// <param name="logger">The logger used to record service operations.</param>
    /// <param name="timeProvider">The time provider used to timestamp pledger updates.</param>
    public PledgerService(
        IPledgerRepository pledgerRepository,
        ILogger<PledgerService> logger,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(pledgerRepository);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _pledgerRepository = pledgerRepository;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task AddAsync(Pledger pledger, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pledger);

        await _pledgerRepository.AddAsync(pledger, cancellationToken);
        _logger.PledgerAdded(pledger.Id);
    }

    /// <inheritdoc />
    public async Task<Pledger?> GetByIdAsync(PledgerId id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.ThrowIfEmptyId(id, "Pledger");

        var pledger = await _pledgerRepository.GetByIdAsync(id, cancellationToken);
        if (pledger is null)
        {
            _logger.PledgerNotFound(id);
            return null;
        }

        _logger.PledgerRetrieved(id);
        return pledger;
    }

    /// <inheritdoc />
    public async Task<PagedPledgersResult> GetAllAsync(
        PledgerQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        query ??= new PledgerQuery();
        var pledgers = await _pledgerRepository.GetAllAsync(query, cancellationToken);
        _logger.PledgersRetrieved(pledgers.Items.Count);
        return pledgers;
    }

    /// <inheritdoc />
    public async Task<Pledger?> UpdateAsync(
        PledgerId id,
        string pledgerName,
        bool active,
        CancellationToken cancellationToken = default)
    {
        ServiceGuard.ThrowIfEmptyId(id, "Pledger");
        // Checked before the lookup so a blank name is rejected (400) even when the ID is unknown (404).
        ArgumentException.ThrowIfNullOrWhiteSpace(pledgerName);

        var pledger = await _pledgerRepository.GetByIdAsync(id, cancellationToken);
        if (pledger is null)
        {
            _logger.PledgerNotFoundForUpdate(id);
            return null;
        }

        pledger.Update(pledgerName, active, _timeProvider);
        await _pledgerRepository.UpdateAsync(pledger, cancellationToken);
        _logger.PledgerUpdated(id);
        return pledger;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(PledgerId id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.ThrowIfEmptyId(id, "Pledger");

        await _pledgerRepository.RemoveAsync(id, cancellationToken);
        _logger.PledgerRemoved(id);
    }
}
