using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.Application.Services;

/// <summary>
/// Defines application operations for managing pledge categories.
/// </summary>
public interface IPledgeCategoryService
{
    /// <summary>
    /// Retrieves all pledge categories.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<IReadOnlyList<PledgeCategory>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a pledge category by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the pledge category to retrieve.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The pledge category, or <see langword="null"/> if no category has the identifier.</returns>
    Task<PledgeCategory?> GetByIdAsync(PledgeCategoryId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a pledge category.
    /// </summary>
    /// <param name="pledgeCategory">The pledge category to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a pledge category's name and active status.
    /// </summary>
    /// <param name="id">The identifier of the pledge category to update.</param>
    /// <param name="categoryName">The updated category name.</param>
    /// <param name="active">Whether the category is active.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The updated category, or <see langword="null"/> if no category has the identifier.</returns>
    Task<PledgeCategory?> UpdateAsync(
        PledgeCategoryId id,
        string categoryName,
        bool active,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a pledge category by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the pledge category to remove.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task RemoveAsync(PledgeCategoryId id, CancellationToken cancellationToken = default);
}
