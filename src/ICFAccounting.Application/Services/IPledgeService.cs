using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.Application.Services;

/// <summary>
/// Application operations for managing pledges.
/// </summary>
public interface IPledgeService
{
    /// <summary>
    /// Adds a pledge.
    /// </summary>
    /// <param name="pledge">The pledge to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(Pledge pledge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a pledge by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the pledge to retrieve.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The pledge, or <see langword="null"/> if no pledge has the identifier.</returns>
    Task<Pledge?> GetByIdAsync(PledgeId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a page of pledges made by a pledger using the specified pagination and sorting options.
    /// </summary>
    /// <param name="pledgerId">The identifier of the pledger whose pledges are retrieved.</param>
    /// <param name="query">The pagination and sorting options, or <see langword="null"/> to use defaults.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<PagedPledgesResult> GetByPledgerAsync(
        PledgerId pledgerId, PledgeQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a page of pledges in a category using the specified pagination and sorting options.
    /// </summary>
    /// <param name="pledgeCategoryId">The identifier of the category whose pledges are retrieved.</param>
    /// <param name="query">The pagination and sorting options, or <see langword="null"/> to use defaults.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<PagedPledgesResult> GetByCategoryAsync(
        PledgeCategoryId pledgeCategoryId, PledgeQuery? query = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a pledge and returns it, or returns null when no pledge has the given id.
    /// </summary>
    /// <param name="id">The identifier of the pledge to update.</param>
    /// <param name="year">The updated pledge year.</param>
    /// <param name="amount">The updated pledge amount.</param>
    /// <param name="pledgerId">The identifier of the pledger associated with the pledge.</param>
    /// <param name="pledgeCategoryId">The identifier of the category associated with the pledge.</param>
    /// <param name="comment">The updated optional comment.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The updated pledge, or <see langword="null"/> if no pledge has the identifier.</returns>
    Task<Pledge?> UpdateAsync(
        PledgeId id,
        int year,
        decimal amount,
        PledgerId pledgerId,
        PledgeCategoryId pledgeCategoryId,
        string? comment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a pledge by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the pledge to remove.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task RemoveAsync(PledgeId id, CancellationToken cancellationToken = default);
}
