using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.Application.Services;

public sealed class PledgerService : IPledgerService
{
    private readonly IPledgerRepository _pledgerRepository;
    private readonly ILogger<PledgerService> _logger;

    public PledgerService(
        IPledgerRepository pledgerRepository,
        ILogger<PledgerService> logger)
    {
        ArgumentNullException.ThrowIfNull(pledgerRepository);
        ArgumentNullException.ThrowIfNull(logger);

        _pledgerRepository = pledgerRepository;
        _logger = logger;
    }

    public async Task AddAsync(Pledger pledger, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pledger);
        ValidatePledgerName(pledger);

        await _pledgerRepository.AddAsync(pledger, cancellationToken);
        _logger.LogInformation("Added pledger {PledgerId}.", pledger.Id);
    }

    public async Task<Pledger?> GetByIdAsync(PledgerId id, CancellationToken cancellationToken = default)
    {
        ValidateId(id);

        var pledger = await _pledgerRepository.GetByIdAsync(id, cancellationToken);
        if (pledger is null)
        {
            _logger.LogInformation("Pledger {PledgerId} was not found.", id);
            return null;
        }

        _logger.LogInformation("Retrieved pledger {PledgerId}.", id);
        return pledger;
    }

    public async Task<PagedPledgersResult> GetAllAsync(
        CancellationToken cancellationToken = default,
        PledgerQuery? query = null)
    {
        query ??= new PledgerQuery();
        var pledgers = await _pledgerRepository.GetAllAsync(cancellationToken, query);
        _logger.LogInformation("Retrieved {PledgerCount} pledgers.", pledgers.Items.Count);
        return pledgers;
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        var count = await _pledgerRepository.CountAsync(cancellationToken);
        _logger.LogInformation("Counted {PledgerCount} pledgers.", count);
        return count;
    }

    public async Task UpdateAsync(Pledger pledger, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pledger);
        ValidatePledgerName(pledger);

        await _pledgerRepository.UpdateAsync(pledger, cancellationToken);
        _logger.LogInformation("Updated pledger {PledgerId}.", pledger.Id);
    }

    public async Task RemoveAsync(PledgerId id, CancellationToken cancellationToken = default)
    {
        ValidateId(id);

        await _pledgerRepository.RemoveAsync(id, cancellationToken);
        _logger.LogInformation("Removed pledger {PledgerId}.", id);
    }

    private static void ValidateId(PledgerId id)
    {
        if (id == default)
        {
            throw new ArgumentException("Pledger ID cannot be empty.", nameof(id));
        }
    }

    private static void ValidatePledgerName(Pledger pledger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pledger.PledgerName);
    }
}
