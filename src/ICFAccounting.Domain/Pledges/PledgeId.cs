namespace ICFAccounting.Domain.Pledges;

public readonly record struct PledgeId(Guid Value)
{
    public static PledgeId New() => new(Guid.NewGuid());
}
