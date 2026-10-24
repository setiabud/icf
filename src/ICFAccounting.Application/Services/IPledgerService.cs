using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.Application.Services;

/// <summary>
/// Defines application operations for managing pledgers.
/// </summary>
public interface IPledgerService
{
    /// <summary>
    /// Adds a pledger.
    /// </summary>
    /// <param name="pledger">The pledger to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(Pledger pledger, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a pledger by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the pledger to retrieve.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The pledger, or <see langword="null"/> if no pledger has the identifier.</returns>
    Task<Pledger?> GetByIdAsync(PledgerId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a page of pledgers using the specified pagination and sorting options.
    /// </summary>
    /// <param name="query">The pagination and sorting options, or <see langword="null"/> to use defaults.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<PagedPledgersResult> GetAllAsync(PledgerQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a pledger's name and active status.
    /// </summary>
    /// <param name="id">The identifier of the pledger to update.</param>
    /// <param name="pledgerName">The updated pledger name.</param>
    /// <param name="active">Whether the pledger is active.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The updated pledger, or <see langword="null"/> if no pledger has the identifier.</returns>
    Task<Pledger?> UpdateAsync(
        PledgerId id, string pledgerName, bool active, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a pledger by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the pledger to remove.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task RemoveAsync(PledgerId id, CancellationToken cancellationToken = default);
}
