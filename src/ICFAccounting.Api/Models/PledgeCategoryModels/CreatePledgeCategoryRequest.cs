namespace ICFAccounting.Api.Models.PledgeCategoryModels;

/// <summary>
/// Request to create a pledge category.
/// </summary>
public sealed record CreatePledgeCategoryRequest
{
    public required string CategoryName { get; init; }
}
