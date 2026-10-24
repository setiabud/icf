using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.Domain.Repositories;

/// <summary>
/// Defines persistence operations for <see cref="Pledger"/> entities.
/// </summary>
public interface IPledgerRepository
{
    /// <summary>
    /// Stores a pledger, replacing any entry with the same identifier.
    /// </summary>
    /// <param name="pledger">The pledger to store.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the persistence operation.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task AddAsync(Pledger pledger, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a pledger by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the pledger to retrieve.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The pledger, or <see langword="null"/> if no entry has the identifier.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task<Pledger?> GetByIdAsync(PledgerId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves pledgers with optional pagination and sorting.
    /// </summary>
    /// <param name="query">
    /// The pagination and sorting options, or <see langword="null"/> to return all entries
    /// in one page without applying sorting.
    /// </param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A snapshot of the selected entries and pagination metadata for all pledgers.</returns>
    /// <remarks>
    /// An explicit query sorts names using an ordinal, case-insensitive comparison,
    /// with ascending identifiers breaking ties regardless of the primary sort direction.
    /// A null query uses page 1 and a page size equal to the total count, or 1 when empty.
    /// The snapshot fixes collection membership, not the state of individual entities.
    /// </remarks>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task<PagedPledgersResult> GetAllAsync(PledgerQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a pledger, inserting it if no entry has the identifier.
    /// </summary>
    /// <param name="pledger">The pledger to store.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the persistence operation.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task UpdateAsync(Pledger pledger, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a pledger by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the pledger to remove.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the removal operation.</returns>
    /// <exception cref="KeyNotFoundException">No pledger has the specified identifier.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task RemoveAsync(PledgerId id, CancellationToken cancellationToken = default);
}
