using ICFAccounting.Api.Models.PledgeCategoryModels;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.Api.Extensions;

public static class PledgeCategoryEndpointExtensions
{
    public static void MapPledgeCategoryEndpoints(this WebApplication app)
    {
        app.MapGet("/pledge-categories/{id:guid}", GetPledgeCategoryByIdAsync)
            .WithName("GetPledgeCategoryById")
            .WithSummary("Get a pledge category by ID")
            .Produces<PledgeCategoryResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GetPledgeCategoryByIdAsync(
        Guid id,
        IPledgeCategoryService pledgeCategoryService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge category ID",
                detail: "Pledge category ID cannot be empty.");
        }

        try
        {
            var pledgeCategory = await pledgeCategoryService.GetByIdAsync(
                new PledgeCategoryId(id), cancellationToken);
            return pledgeCategory is null
                ? Results.NotFound()
                : Results.Ok(ToPledgeCategoryResponse(pledgeCategory));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge category ID",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception, "An unexpected error occurred while retrieving pledge category {PledgeCategoryId}.", id);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledge category could not be retrieved.");
        }
    }

    private static PledgeCategoryResponse ToPledgeCategoryResponse(PledgeCategory pledgeCategory)
    {
        return new PledgeCategoryResponse
        {
            Id = pledgeCategory.Id.Value,
            CategoryName = pledgeCategory.CategoryName,
            Active = pledgeCategory.Active,
            CreatedAt = pledgeCategory.CreatedAt,
            UpdatedAt = pledgeCategory.UpdatedAt
        };
    }
}
