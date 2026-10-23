using ICFAccounting.Api.Models;
using ICFAccounting.Api.Models.PledgerModels;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.Api.Extensions;

public static class PledgerEndpointExtensions
{
    public static void MapPledgerEndpoints(this WebApplication app)
    {
        app.MapPost("/pledgers", CreatePledgerAsync)
            .WithName("CreatePledger")
            .WithSummary("Create a pledger")
            .Produces<PledgerResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapGet("/pledgers", GetPledgersAsync)
            .WithName("GetPledgers")
            .WithSummary("Get paginated pledgers sorted by name")
            .Produces<PledgerPageResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapGet("/pledgers/{id:guid}", GetPledgerByIdAsync)
            .WithName("GetPledgerById")
            .WithSummary("Get a pledger by ID")
            .Produces<PledgerResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapPut("/pledgers/{id:guid}", UpdatePledgerAsync)
            .WithName("UpdatePledger")
            .WithSummary("Update a pledger")
            .Produces<PledgerResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapDelete("/pledgers/{id:guid}", RemovePledgerAsync)
            .WithName("RemovePledger")
            .WithSummary("Remove a pledger")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> RemovePledgerAsync(
        Guid id,
        IPledgerService pledgerService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledger ID",
                detail: "Pledger ID cannot be empty.");
        }

        try
        {
            await pledgerService.RemoveAsync(new PledgerId(id), cancellationToken);
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
                title: "Invalid pledger ID",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "An unexpected error occurred while removing pledger {PledgerId}.", id);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledger could not be removed.");
        }
    }

    private static async Task<IResult> UpdatePledgerAsync(
        Guid id,
        UpdatePledgerRequest request,
        IPledgerService pledgerService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledger ID",
                detail: "Pledger ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.PledgerName) || request.Active is not { } active)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledger update",
                detail: "PledgerName is required and cannot be empty or whitespace, and Active is required.");
        }

        try
        {
            var pledger = await pledgerService.UpdateAsync(
                new PledgerId(id), request.PledgerName, active, cancellationToken);
            return pledger is null
                ? Results.NotFound()
                : Results.Ok(ToPledgerResponse(pledger));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledger update",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "An unexpected error occurred while updating pledger {PledgerId}.", id);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledger could not be updated.");
        }
    }

    private static async Task<IResult> GetPledgerByIdAsync(
        Guid id,
        IPledgerService pledgerService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledger ID",
                detail: "Pledger ID cannot be empty.");
        }

        try
        {
            var pledger = await pledgerService.GetByIdAsync(new PledgerId(id), cancellationToken);
            return pledger is null
                ? Results.NotFound()
                : Results.Ok(ToPledgerResponse(pledger));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledger ID",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "An unexpected error occurred while retrieving pledger {PledgerId}.", id);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledger could not be retrieved.");
        }
    }

    private static async Task<IResult> GetPledgersAsync(
        IPledgerService pledgerService,
        ILogger<Program> logger,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 10,
        string sortBy = nameof(PledgerSortBy.PledgerName),
        string sortOrder = nameof(SortOrder.Ascending))
    {
        if (!Enum.TryParse<PledgerSortBy>(sortBy, true, out var parsedSortBy)
            || !Enum.IsDefined(parsedSortBy)
            || !string.Equals(sortBy, parsedSortBy.ToString(), StringComparison.OrdinalIgnoreCase)
            || !Enum.TryParse<SortOrder>(sortOrder, true, out var parsedSortOrder)
            || !Enum.IsDefined(parsedSortOrder)
            || !string.Equals(sortOrder, parsedSortOrder.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledger query",
                detail: "Sort By is invalid and SortOrder must be Ascending or Descending.");
        }

        PledgerQuery query;
        try
        {
            query = new PledgerQuery(page, pageSize, parsedSortBy, parsedSortOrder);
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledger query",
                detail: exception.Message);
        }

        try
        {
            var pledgers = await pledgerService.GetAllAsync(cancellationToken, query);
            return Results.Ok(new PledgerPageResponse
            {
                Items = pledgers.Items.Select(ToPledgerResponse).ToArray(),
                Page = pledgers.Page,
                PageSize = pledgers.PageSize,
                TotalCount = pledgers.TotalCount,
                TotalPages = pledgers.TotalPages
            });
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledger query",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "An unexpected error occurred while retrieving pledgers.");
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledgers could not be retrieved.");
        }
    }

    private static async Task<IResult> CreatePledgerAsync(
        CreatePledgerRequest request,
        IPledgerService pledgerService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PledgerName))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledger name",
                detail: "PledgerName is required and cannot be empty or whitespace.");
        }

        try
        {
            var pledger = Pledger.Create(request.PledgerName);
            await pledgerService.AddAsync(pledger, cancellationToken);

            return Results.Created($"/pledgers/{pledger.Id.Value}", ToPledgerResponse(pledger));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledger",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "An unexpected error occurred while creating a pledger.");
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledger could not be created.");
        }
    }

    private static PledgerResponse ToPledgerResponse(Pledger pledger)
    {
        return new PledgerResponse
        {
            Id = pledger.Id.Value,
            PledgerName = pledger.PledgerName,
            Active = pledger.Active,
            CreatedAt = pledger.CreatedAt,
            UpdatedAt = pledger.UpdatedAt
        };
    }
}