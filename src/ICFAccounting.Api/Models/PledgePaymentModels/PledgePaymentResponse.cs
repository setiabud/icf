namespace ICFAccounting.Api.Models.PledgePaymentModels;

/// <summary>
/// Pledge payment details returned by the API.
/// </summary>
public sealed record PledgePaymentResponse
{
    public Guid Id { get; init; }

    public Guid PledgeId { get; init; }

    public decimal Amount { get; init; }

    public DateTime PaymentDate { get; init; }

    public string? Comment { get; init; }

    public bool Active { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}
