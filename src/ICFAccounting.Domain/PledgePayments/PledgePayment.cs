using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.Domain.PledgePayments;

/// <summary>
/// A payment made toward a pledge, associated by pledge identity.
/// </summary>
public sealed class PledgePayment
{
    private PledgePayment(
        PledgePaymentId id,
        PledgeId pledgeId,
        decimal amount,
        DateTime paymentDate,
        string? comment,
        bool active,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        PledgeId = pledgeId;
        Amount = amount;
        PaymentDate = paymentDate;
        Comment = comment;
        Active = active;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public PledgePaymentId Id { get; }

    public PledgeId PledgeId { get; private set; }

    public decimal Amount { get; private set; }

    public DateTime PaymentDate { get; private set; }

    public string? Comment { get; private set; }

    public bool Active { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Creates an active pledge payment with a positive amount of at most two decimal places.
    /// </summary>
    public static PledgePayment Create(
        PledgeId pledgeId,
        decimal amount,
        DateTime paymentDate,
        string? comment = null,
        TimeProvider? timeProvider = null)
    {
        Validate(pledgeId, amount, paymentDate);

        var createdAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        return new PledgePayment(
            PledgePaymentId.New(),
            pledgeId,
            amount,
            paymentDate,
            NormalizeComment(comment),
            true,
            createdAt,
            createdAt);
    }

    /// <summary>
    /// Revalidates and replaces payment details and pledge association while preserving identity.
    /// </summary>
    public void Update(
        PledgeId pledgeId,
        decimal amount,
        DateTime paymentDate,
        string? comment,
        bool active,
        TimeProvider? timeProvider = null)
    {
        Validate(pledgeId, amount, paymentDate);

        PledgeId = pledgeId;
        Amount = amount;
        PaymentDate = paymentDate;
        Comment = NormalizeComment(comment);
        Active = active;
        UpdatedAt = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
    }

    private static void Validate(PledgeId pledgeId, decimal amount, DateTime paymentDate)
    {
        if (pledgeId == default)
        {
            throw new ArgumentException("Pledge ID cannot be empty.", nameof(pledgeId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException("Amount must have at most two decimal places.", nameof(amount));
        }

        if (paymentDate == default)
        {
            throw new ArgumentException("Payment date cannot be empty.", nameof(paymentDate));
        }
    }

    private static string? NormalizeComment(string? comment)
    {
        return string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }
}