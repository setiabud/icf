namespace ICFAccounting.Api.Models.PledgePaymentModels;

/// <summary>
/// Request to update a pledge payment and its associated pledge.
/// Nullable properties allow missing required fields to be rejected with a ProblemDetails response.
/// </summary>
public sealed record UpdatePledgePaymentRequest
{
    public Guid? PledgeId { get; init; }

    public decimal? Amount { get; init; }

    public DateTime? PaymentDate { get; init; }

    public string? Comment { get; init; }

    public bool? Active { get; init; }
}
