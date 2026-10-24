using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.Domain.Repositories;

/// <summary>
/// Defines persistence operations for <see cref="PledgeCategory"/> entities.
/// </summary>
public interface IPledgeCategoryRepository
{
    /// <summary>
    /// Stores a pledge category, replacing any entry with the same identifier.
    /// </summary>
    /// <param name="pledgeCategory">The pledge category to store.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the persistence operation.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task AddAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a pledge category by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the pledge category to retrieve.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The category, or <see langword="null"/> if no entry has the identifier.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task<PledgeCategory?> GetByIdAsync(PledgeCategoryId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all pledge categories without applying sorting.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A snapshot containing all pledge categories.</returns>
    /// <remarks>The snapshot fixes collection membership, not the state of individual entities.</remarks>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task<IReadOnlyList<PledgeCategory>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a pledge category, inserting it if no entry has the identifier.
    /// </summary>
    /// <param name="pledgeCategory">The pledge category to store.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the persistence operation.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task UpdateAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a pledge category by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the pledge category to remove.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the removal operation.</returns>
    /// <exception cref="KeyNotFoundException">No pledge category has the specified identifier.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task RemoveAsync(PledgeCategoryId id, CancellationToken cancellationToken = default);
}
