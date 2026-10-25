using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.Domain.Repositories;

/// <summary>
/// Persistence port for <see cref="PledgePayment"/> entities.
/// </summary>
public interface IPledgePaymentRepository
{
    /// <summary>
    /// Stores a pledge payment, replacing any entry with the same identifier.
    /// </summary>
    /// <param name="pledgePayment">The payment to store.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the persistence operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pledgePayment"/> is null.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task AddAsync(PledgePayment pledgePayment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a pledge payment by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the payment to retrieve.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The payment, or <see langword="null"/> if no entry has the identifier.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task<PledgePayment?> GetByIdAsync(
        PledgePaymentId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a page of payments made toward a pledge.
    /// </summary>
    /// <param name="pledgeId">The identifier of the pledge whose payments are retrieved.</param>
    /// <param name="query">
    /// Pagination and sorting options, or <see langword="null"/> to use
    /// <see cref="PledgePaymentQuery"/> defaults: page 1, page size 20, and descending payment date.
    /// </param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A page of matching payments and pagination metadata for the complete result set.</returns>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task<PagedPledgePaymentsResult> GetByPledgeAsync(
        PledgeId pledgeId,
        PledgePaymentQuery? query = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a pledge payment, inserting it if no entry has the identifier.
    /// </summary>
    /// <param name="pledgePayment">The payment to store.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the persistence operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pledgePayment"/> is null.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task UpdateAsync(PledgePayment pledgePayment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a pledge payment by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the payment to remove.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the removal operation.</returns>
    /// <exception cref="KeyNotFoundException">No payment has the specified identifier.</exception>
    /// <exception cref="OperationCanceledException">The cancellation token has been cancelled.</exception>
    Task RemoveAsync(PledgePaymentId id, CancellationToken cancellationToken = default);
}