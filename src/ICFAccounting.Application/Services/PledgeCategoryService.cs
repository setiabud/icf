using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.Application.Services;

/// <summary>
/// Implements application operations for managing pledge categories.
/// </summary>
public sealed class PledgeCategoryService : IPledgeCategoryService
{
    private readonly IPledgeCategoryRepository _pledgeCategoryRepository;
    private readonly ILogger<PledgeCategoryService> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PledgeCategoryService"/> class.
    /// </summary>
    /// <param name="pledgeCategoryRepository">The repository used to retrieve and persist pledge categories.</param>
    /// <param name="logger">The logger used to record service operations.</param>
    /// <param name="timeProvider">The time provider used to timestamp category updates.</param>
    public PledgeCategoryService(
        IPledgeCategoryRepository pledgeCategoryRepository,
        ILogger<PledgeCategoryService> logger,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(pledgeCategoryRepository);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _pledgeCategoryRepository = pledgeCategoryRepository;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PledgeCategory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var pledgeCategories = await _pledgeCategoryRepository.GetAllAsync(cancellationToken);
        _logger.PledgeCategoriesRetrieved(pledgeCategories.Count);
        return pledgeCategories;
    }

    /// <inheritdoc />
    public async Task<PledgeCategory?> GetByIdAsync(PledgeCategoryId id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.ThrowIfEmptyId(id, "Pledge category");

        var pledgeCategory = await _pledgeCategoryRepository.GetByIdAsync(id, cancellationToken);
        if (pledgeCategory is null)
        {
            _logger.PledgeCategoryNotFound(id);
            return null;
        }

        _logger.PledgeCategoryRetrieved(id);
        return pledgeCategory;
    }

    /// <inheritdoc />
    public async Task AddAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pledgeCategory);

        await _pledgeCategoryRepository.AddAsync(pledgeCategory, cancellationToken);
        _logger.PledgeCategoryAdded(pledgeCategory.Id);
    }

    /// <inheritdoc />
    public async Task<PledgeCategory?> UpdateAsync(
        PledgeCategoryId id,
        string categoryName,
        bool active,
        CancellationToken cancellationToken = default)
    {
        ServiceGuard.ThrowIfEmptyId(id, "Pledge category");
        // Checked before the lookup so a blank name is rejected (400) even when the ID is unknown (404).
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);

        var pledgeCategory = await _pledgeCategoryRepository.GetByIdAsync(id, cancellationToken);
        if (pledgeCategory is null)
        {
            _logger.PledgeCategoryNotFoundForUpdate(id);
            return null;
        }

        pledgeCategory.Update(categoryName, active, _timeProvider);
        await _pledgeCategoryRepository.UpdateAsync(pledgeCategory, cancellationToken);
        _logger.PledgeCategoryUpdated(id);
        return pledgeCategory;
    }

    /// <inheritdoc />
    public async Task RemoveAsync(PledgeCategoryId id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.ThrowIfEmptyId(id, "Pledge category");

        await _pledgeCategoryRepository.RemoveAsync(id, cancellationToken);
        _logger.PledgeCategoryRemoved(id);
    }
}
