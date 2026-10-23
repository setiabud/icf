using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.Application.Services;

public sealed class PledgeCategoryService : IPledgeCategoryService
{
    private readonly IPledgeCategoryRepository _pledgeCategoryRepository;
    private readonly ILogger<PledgeCategoryService> _logger;

    public PledgeCategoryService(
        IPledgeCategoryRepository pledgeCategoryRepository,
        ILogger<PledgeCategoryService> logger)
    {
        ArgumentNullException.ThrowIfNull(pledgeCategoryRepository);
        ArgumentNullException.ThrowIfNull(logger);

        _pledgeCategoryRepository = pledgeCategoryRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PledgeCategory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var pledgeCategories = await _pledgeCategoryRepository.GetAllAsync(cancellationToken);
        IReadOnlyList<PledgeCategory> result = [.. pledgeCategories];
        _logger.LogInformation("Retrieved {PledgeCategoryCount} pledge categories.", result.Count);
        return result;
    }

    public async Task<PledgeCategory?> GetByIdAsync(PledgeCategoryId id, CancellationToken cancellationToken = default)
    {
        ValidateId(id);

        var pledgeCategory = await _pledgeCategoryRepository.GetByIdAsync(id, cancellationToken);
        if (pledgeCategory is null)
        {
            _logger.LogInformation("Pledge category {PledgeCategoryId} was not found.", id);
            return null;
        }

        _logger.LogInformation("Retrieved pledge category {PledgeCategoryId}.", id);
        return pledgeCategory;
    }

    public async Task AddAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pledgeCategory);
        ValidateCategoryName(pledgeCategory);

        await _pledgeCategoryRepository.AddAsync(pledgeCategory, cancellationToken);
        _logger.LogInformation("Added pledge category {PledgeCategoryId}.", pledgeCategory.Id);
    }

    public async Task<PledgeCategory?> UpdateAsync(
        PledgeCategoryId id,
        string categoryName,
        bool active,
        CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(categoryName);

        var pledgeCategory = await _pledgeCategoryRepository.GetByIdAsync(id, cancellationToken);
        if (pledgeCategory is null)
        {
            _logger.LogInformation("Pledge category {PledgeCategoryId} was not found for update.", id);
            return null;
        }

        pledgeCategory.Update(categoryName, active);
        await _pledgeCategoryRepository.UpdateAsync(pledgeCategory, cancellationToken);
        _logger.LogInformation("Updated pledge category {PledgeCategoryId}.", id);
        return pledgeCategory;
    }

    public async Task RemoveAsync(PledgeCategoryId id, CancellationToken cancellationToken = default)
    {
        ValidateId(id);

        await _pledgeCategoryRepository.RemoveAsync(id, cancellationToken);
        _logger.LogInformation("Removed pledge category {PledgeCategoryId}.", id);
    }

    private static void ValidateId(PledgeCategoryId id)
    {
        if (id == default)
        {
            throw new ArgumentException("Pledge category ID cannot be empty.", nameof(id));
        }
    }

    private static void ValidateCategoryName(PledgeCategory pledgeCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pledgeCategory.CategoryName);
    }
}
