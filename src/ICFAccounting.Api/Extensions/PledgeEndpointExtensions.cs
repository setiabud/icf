using ICFAccounting.Api.Models.PledgeModels;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.Api.Extensions;

public static class PledgeEndpointExtensions
{
    public static void MapPledgeEndpoints(this WebApplication app)
    {
        app.MapPost("/pledges", CreatePledgeAsync)
            .WithName("CreatePledge")
            .WithSummary("Create a pledge")
            .Produces<PledgeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapGet("/pledges/{id:guid}", GetPledgeByIdAsync)
            .WithName("GetPledgeById")
            .WithSummary("Get a pledge by ID")
            .Produces<PledgeResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapGet("/pledges/pledger/{pledgerId:guid}", GetPledgesByPledgerAsync)
            .WithName("GetPledgesByPledger")
            .WithSummary("Get a pledger's paginated pledges")
            .Produces<PledgePageResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapGet("/pledges/pledge-category/{pledgeCategoryId:guid}", GetPledgesByCategoryAsync)
            .WithName("GetPledgesByCategory")
            .WithSummary("Get a pledge category's paginated pledges")
            .Produces<PledgePageResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapPut("/pledges/{id:guid}", UpdatePledgeAsync)
            .WithName("UpdatePledge")
            .WithSummary("Update a pledge")
            .Produces<PledgeResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapDelete("/pledges/{id:guid}", RemovePledgeAsync)
            .WithName("RemovePledge")
            .WithSummary("Remove a pledge")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> CreatePledgeAsync(
        CreatePledgeRequest request,
        IPledgeService pledgeService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var pledge = Pledge.Create(
                request.Year,
                request.Amount,
                new PledgerId(request.PledgerId),
                new PledgeCategoryId(request.PledgeCategoryId),
                request.Comment);
            await pledgeService.AddAsync(pledge, cancellationToken);

            return Results.Created($"/pledges/{pledge.Id.Value}", ToPledgeResponse(pledge));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "An unexpected error occurred while creating a pledge.");
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledge could not be created.");
        }
    }

    private static async Task<IResult> GetPledgesByPledgerAsync(
        Guid pledgerId,
        IPledgeService pledgeService,
        ILogger<Program> logger,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        string sortBy = nameof(PledgeSortBy.Year),
        string sortOrder = nameof(SortOrder.Descending))
    {
        if (pledgerId == Guid.Empty)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledger ID",
                detail: "Pledger ID cannot be empty.");
        }

        if (!Enum.TryParse<PledgeSortBy>(sortBy, true, out var parsedSortBy)
            || !Enum.IsDefined(parsedSortBy)
            || !string.Equals(sortBy, parsedSortBy.ToString(), StringComparison.OrdinalIgnoreCase)
            || !Enum.TryParse<SortOrder>(sortOrder, true, out var parsedSortOrder)
            || !Enum.IsDefined(parsedSortOrder)
            || !string.Equals(sortOrder, parsedSortOrder.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge query",
                detail: "SortBy must be Year or Amount, and SortOrder must be Ascending or Descending.");
        }

        PledgeQuery query;
        try
        {
            query = new PledgeQuery(page, pageSize, parsedSortBy, parsedSortOrder);
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge query",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }

        try
        {
            var pledges = await pledgeService.GetByPledgerAsync(
                new PledgerId(pledgerId), query, cancellationToken);
            return Results.Ok(new PledgePageResponse
            {
                Items = pledges.Items.Select(ToPledgeResponse).ToArray(),
                Page = pledges.Page,
                PageSize = pledges.PageSize,
                TotalCount = pledges.TotalCount,
                TotalPages = pledges.TotalPages
            });
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge query",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "An unexpected error occurred while retrieving pledges for pledger {PledgerId}.",
                pledgerId);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledges could not be retrieved.");
        }
    }

    private static async Task<IResult> GetPledgesByCategoryAsync(
        Guid pledgeCategoryId,
        IPledgeService pledgeService,
        ILogger<Program> logger,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        string sortBy = nameof(PledgeSortBy.Year),
        string sortOrder = nameof(SortOrder.Descending))
    {
        if (pledgeCategoryId == Guid.Empty)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge category ID",
                detail: "Pledge category ID cannot be empty.");
        }

        if (!Enum.TryParse<PledgeSortBy>(sortBy, true, out var parsedSortBy)
            || !Enum.IsDefined(parsedSortBy)
            || !string.Equals(sortBy, parsedSortBy.ToString(), StringComparison.OrdinalIgnoreCase)
            || !Enum.TryParse<SortOrder>(sortOrder, true, out var parsedSortOrder)
            || !Enum.IsDefined(parsedSortOrder)
            || !string.Equals(sortOrder, parsedSortOrder.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge query",
                detail: "SortBy must be Year or Amount, and SortOrder must be Ascending or Descending.");
        }

        PledgeQuery query;
        try
        {
            query = new PledgeQuery(page, pageSize, parsedSortBy, parsedSortOrder);
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge query",
                detail: exception.Message);
        }

        try
        {
            var pledges = await pledgeService.GetByCategoryAsync(
                new PledgeCategoryId(pledgeCategoryId), query, cancellationToken);
            return Results.Ok(new PledgePageResponse
            {
                Items = pledges.Items.Select(ToPledgeResponse).ToArray(),
                Page = pledges.Page,
                PageSize = pledges.PageSize,
                TotalCount = pledges.TotalCount,
                TotalPages = pledges.TotalPages
            });
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge query",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "An unexpected error occurred while retrieving pledges for category {PledgeCategoryId}.",
                pledgeCategoryId);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledges could not be retrieved.");
        }
    }

    private static async Task<IResult> UpdatePledgeAsync(
        Guid id,
        UpdatePledgeRequest request,
        IPledgeService pledgeService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge ID",
                detail: "Pledge ID cannot be empty.");
        }

        if (request.Year is not { } year
            || request.Amount is not { } amount
            || request.PledgerId is not { } pledgerId
            || request.PledgeCategoryId is not { } pledgeCategoryId)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge update",
                detail: "Year, Amount, PledgerId, and PledgeCategoryId are required.");
        }

        if (pledgerId == Guid.Empty || pledgeCategoryId == Guid.Empty)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge update",
                detail: "PledgerId and PledgeCategoryId cannot be empty.");
        }

        try
        {
            var pledge = await pledgeService.UpdateAsync(
                new PledgeId(id),
                year,
                amount,
                new PledgerId(pledgerId),
                new PledgeCategoryId(pledgeCategoryId),
                request.Comment,
                cancellationToken);
            return pledge is null
                ? Results.NotFound()
                : Results.Ok(ToPledgeResponse(pledge));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge update",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "An unexpected error occurred while updating pledge {PledgeId}.", id);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledge could not be updated.");
        }
    }

    private static async Task<IResult> GetPledgeByIdAsync(
        Guid id,
        IPledgeService pledgeService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge ID",
                detail: "Pledge ID cannot be empty.");
        }

        try
        {
            var pledge = await pledgeService.GetByIdAsync(new PledgeId(id), cancellationToken);
            return pledge is null
                ? Results.NotFound()
                : Results.Ok(ToPledgeResponse(pledge));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge ID",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "An unexpected error occurred while retrieving pledge {PledgeId}.", id);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledge could not be retrieved.");
        }
    }

    private static async Task<IResult> RemovePledgeAsync(
        Guid id,
        IPledgeService pledgeService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge ID",
                detail: "Pledge ID cannot be empty.");
        }

        try
        {
            await pledgeService.RemoveAsync(new PledgeId(id), cancellationToken);
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
                title: "Invalid pledge ID",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "An unexpected error occurred while removing pledge {PledgeId}.", id);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledge could not be removed.");
        }
    }

    private static PledgeResponse ToPledgeResponse(Pledge pledge)
    {
        return new PledgeResponse
        {
            Id = pledge.Id.Value,
            Year = pledge.Year,
            Amount = pledge.Amount,
            PledgerId = pledge.PledgerId.Value,
            PledgeCategoryId = pledge.PledgeCategoryId.Value,
            Comment = pledge.Comment,
            CreatedAt = pledge.CreatedAt,
            UpdatedAt = pledge.UpdatedAt
        };
    }
}