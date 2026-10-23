namespace ICFAccounting.Domain.PledgeCategories;

public readonly record struct PledgeCategoryId(Guid Value)
{
    public static PledgeCategoryId New() => new(Guid.NewGuid());
}
