using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.Application.Services;

/// <summary>
/// Application operations for managing pledge payments.
/// </summary>
public interface IPledgePaymentService
{
    /// <summary>
    /// Adds a pledge payment.
    /// </summary>
    /// <param name="pledgePayment">The payment to add.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(PledgePayment pledgePayment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a pledge payment by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the payment to retrieve.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The payment, or <see langword="null"/> if no payment has the identifier.</returns>
    Task<PledgePayment?> GetByIdAsync(
        PledgePaymentId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a page of payments made toward a pledge.
    /// </summary>
    /// <param name="pledgeId">The identifier of the pledge whose payments are retrieved.</param>
    /// <param name="query">Pagination and sorting options, or <see langword="null"/> to use defaults.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task<PagedPledgePaymentsResult> GetByPledgeAsync(
        PledgeId pledgeId,
        PledgePaymentQuery? query = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a payment and returns it, or returns null when no payment has the given identifier.
    /// </summary>
    /// <param name="id">The identifier of the payment to update.</param>
    /// <param name="amount">The updated payment amount.</param>
    /// <param name="paymentDate">The updated payment date.</param>
    /// <param name="comment">The updated optional comment.</param>
    /// <param name="active">Whether the payment is active.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The updated payment, or <see langword="null"/> if no payment has the identifier.</returns>
    Task<PledgePayment?> UpdateAsync(
        PledgePaymentId id,
        decimal amount,
        DateTime paymentDate,
        string? comment,
        bool active,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a pledge payment by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the payment to remove.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task RemoveAsync(PledgePaymentId id, CancellationToken cancellationToken = default);
}