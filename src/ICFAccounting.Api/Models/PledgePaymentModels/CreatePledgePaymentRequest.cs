namespace ICFAccounting.Api.Models.PledgePaymentModels;

/// <summary>
/// Request to create a pledge payment.
/// </summary>
public sealed record CreatePledgePaymentRequest
{
    public required Guid PledgeId { get; init; }

    public required decimal Amount { get; init; }

    public required DateTime PaymentDate { get; init; }

    public string? Comment { get; init; }
}
