namespace ICFAccounting.Api.Models.PledgeCategoryModels;

/// <summary>
/// Request to update a pledge category.
/// Properties are nullable so missing fields can be rejected with a ProblemDetails response.
/// </summary>
public sealed record UpdatePledgeCategoryRequest
{
    public string? CategoryName { get; init; }

    public bool? Active { get; init; }
}
