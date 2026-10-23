namespace ICFAccounting.Domain.Pledgers;

public readonly record struct PledgerId(Guid Value)
{
    public static PledgerId New() => new(Guid.NewGuid());
}
