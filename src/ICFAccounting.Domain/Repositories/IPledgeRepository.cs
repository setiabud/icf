using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.Domain.Repositories;

/// <summary>
/// Persistence port for <see cref="Pledge"/> entities.
/// </summary>
public interface IPledgeRepository
{
    /// <summary>
    /// Stores a pledge, replacing any entry with the same identifier.
    /// </summary>
    /// <param name="pledge">The pledge to store.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the persistence operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pledge"/> is null.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task AddAsync(Pledge pledge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a pledge by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the pledge to retrieve.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The pledge, or <see langword="null"/> if no entry has the identifier.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task<Pledge?> GetByIdAsync(PledgeId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a page of pledges made by a pledger.
    /// </summary>
    /// <param name="pledgerId">The identifier of the pledger whose pledges are retrieved.</param>
    /// <param name="query">
    /// The pagination and sorting options, or <see langword="null"/> to use
    /// <see cref="PledgeQuery"/> defaults: page 1, page size 20, and descending year order.
    /// </param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A snapshot of the selected entries and pagination metadata for all matching pledges.</returns>
    /// <remarks>
    /// Descending identifiers break year ties regardless of the primary sort direction.
    /// The snapshot fixes collection membership, not the state of individual entities.
    /// </remarks>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task<PagedPledgesResult> GetByPledgerAsync(
        PledgerId pledgerId, PledgeQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a page of pledges in a category.
    /// </summary>
    /// <param name="pledgeCategoryId">The identifier of the category whose pledges are retrieved.</param>
    /// <param name="query">
    /// The pagination and sorting options, or <see langword="null"/> to use
    /// <see cref="PledgeQuery"/> defaults: page 1, page size 20, and descending year order.
    /// </param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A snapshot of the selected entries and pagination metadata for all matching pledges.</returns>
    /// <remarks>
    /// Descending identifiers break year ties regardless of the primary sort direction.
    /// The snapshot fixes collection membership, not the state of individual entities.
    /// </remarks>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task<PagedPledgesResult> GetByCategoryAsync(
        PledgeCategoryId pledgeCategoryId, PledgeQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a pledge, inserting it if no entry has the identifier.
    /// </summary>
    /// <param name="pledge">The pledge to store.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the persistence operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pledge"/> is null.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task UpdateAsync(Pledge pledge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a pledge by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the pledge to remove.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the removal operation.</returns>
    /// <exception cref="KeyNotFoundException">No pledge has the specified identifier.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task RemoveAsync(PledgeId id, CancellationToken cancellationToken = default);
}
