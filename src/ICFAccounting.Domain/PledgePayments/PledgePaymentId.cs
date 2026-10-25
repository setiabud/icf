namespace ICFAccounting.Domain.PledgePayments;

public readonly record struct PledgePaymentId(Guid Value)
{
    public static PledgePaymentId New() => new(Guid.NewGuid());
}