using ICFAccounting.Api.Models.PledgeCategoryModels;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.Api.Extensions;

public static class PledgeCategoryEndpointExtensions
{
    public static void MapPledgeCategoryEndpoints(this WebApplication app)
    {
        app.MapPost("/pledge-categories", CreatePledgeCategoryAsync)
            .WithName("CreatePledgeCategory")
            .WithSummary("Create a pledge category")
            .Produces<PledgeCategoryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapGet("/pledge-categories", GetPledgeCategoriesAsync)
            .WithName("GetPledgeCategories")
            .WithSummary("Get all pledge categories")
            .Produces<PledgeCategoryResponse[]>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapGet("/pledge-categories/{id:guid}", GetPledgeCategoryByIdAsync)
            .WithName("GetPledgeCategoryById")
            .WithSummary("Get a pledge category by ID")
            .Produces<PledgeCategoryResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapPut("/pledge-categories/{id:guid}", UpdatePledgeCategoryAsync)
            .WithName("UpdatePledgeCategory")
            .WithSummary("Update a pledge category")
            .Produces<PledgeCategoryResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapDelete("/pledge-categories/{id:guid}", RemovePledgeCategoryAsync)
            .WithName("RemovePledgeCategory")
            .WithSummary("Remove a pledge category")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> CreatePledgeCategoryAsync(
        CreatePledgeCategoryRequest request,
        IPledgeCategoryService pledgeCategoryService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CategoryName))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge category name",
                detail: "CategoryName is required and cannot be empty or whitespace.");
        }

        try
        {
            var pledgeCategory = PledgeCategory.Create(request.CategoryName);
            await pledgeCategoryService.AddAsync(pledgeCategory, cancellationToken);

            return Results.Created(
                $"/pledge-categories/{pledgeCategory.Id.Value}", ToPledgeCategoryResponse(pledgeCategory));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge category",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "An unexpected error occurred while creating a pledge category.");
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledge category could not be created.");
        }
    }

    private static async Task<IResult> GetPledgeCategoriesAsync(
        IPledgeCategoryService pledgeCategoryService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var pledgeCategories = await pledgeCategoryService.GetAllAsync(cancellationToken);
            return Results.Ok(pledgeCategories.Select(ToPledgeCategoryResponse).ToArray());
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge category query",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "An unexpected error occurred while retrieving pledge categories.");
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledge categories could not be retrieved.");
        }
    }

    private static async Task<IResult> RemovePledgeCategoryAsync(
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
            await pledgeCategoryService.RemoveAsync(new PledgeCategoryId(id), cancellationToken);
            return Results.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
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
                exception, "An unexpected error occurred while removing pledge category {PledgeCategoryId}.", id);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledge category could not be removed.");
        }
    }

    private static async Task<IResult> UpdatePledgeCategoryAsync(
        Guid id,
        UpdatePledgeCategoryRequest request,
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

        if (string.IsNullOrWhiteSpace(request.CategoryName) || request.Active is not { } active)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge category update",
                detail: "CategoryName is required and cannot be empty or whitespace, and Active is required.");
        }

        try
        {
            var pledgeCategory = await pledgeCategoryService.UpdateAsync(
                new PledgeCategoryId(id), request.CategoryName, active, cancellationToken);
            return pledgeCategory is null
                ? Results.NotFound()
                : Results.Ok(ToPledgeCategoryResponse(pledgeCategory));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge category update",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception, "An unexpected error occurred while updating pledge category {PledgeCategoryId}.", id);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledge category could not be updated.");
        }
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
