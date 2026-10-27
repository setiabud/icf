using ICFAccounting.Api.Models.PledgePaymentModels;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;

namespace ICFAccounting.Api.Extensions;

public static class PledgePaymentEndpointExtensions
{
    public static void MapPledgePaymentEndpoints(this WebApplication app)
    {
        app.MapPost("/pledge-payments", CreatePledgePaymentAsync)
            .WithName("CreatePledgePayment")
            .WithSummary("Create a pledge payment")
            .Produces<PledgePaymentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapGet("/pledge-payments/{id:guid}", GetPledgePaymentByIdAsync)
            .WithName("GetPledgePaymentById")
            .WithSummary("Get a pledge payment by ID")
            .Produces<PledgePaymentResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapGet("/pledge-payments/pledge/{pledgeId:guid}", GetPledgePaymentsByPledgeAsync)
            .WithName("GetPledgePaymentsByPledge")
            .WithSummary("Get a pledge's paginated payments")
            .Produces<PledgePaymentPageResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapPut("/pledge-payments/{id:guid}", UpdatePledgePaymentAsync)
            .WithName("UpdatePledgePayment")
            .WithSummary("Update a pledge payment")
            .Produces<PledgePaymentResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapDelete("/pledge-payments/{id:guid}", RemovePledgePaymentAsync)
            .WithName("RemovePledgePayment")
            .WithSummary("Remove a pledge payment")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> CreatePledgePaymentAsync(
        CreatePledgePaymentRequest request,
        IPledgePaymentService pledgePaymentService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var pledgePayment = PledgePayment.Create(
                new PledgeId(request.PledgeId), request.Amount, request.PaymentDate, request.Comment);
            await pledgePaymentService.AddAsync(pledgePayment, cancellationToken);

            return Results.Created(
                $"/pledge-payments/{pledgePayment.Id.Value}", ToPledgePaymentResponse(pledgePayment));
        }
            catch (RelationshipConflictException exception)
            {
                return ProblemDetailsHelpers.RelationshipConflict(exception);
            }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge payment",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "An unexpected error occurred while creating a pledge payment.");
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledge payment could not be created.");
        }
    }

    private static async Task<IResult> GetPledgePaymentsByPledgeAsync(
        Guid pledgeId,
        IPledgePaymentService pledgePaymentService,
        ILogger<Program> logger,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        string sortBy = nameof(PledgePaymentSortBy.PaymentDate),
        string sortOrder = nameof(SortOrder.Descending))
    {
        if (pledgeId == Guid.Empty)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge ID",
                detail: "Pledge ID cannot be empty.");
        }

        if (!Enum.TryParse<PledgePaymentSortBy>(sortBy, true, out var parsedSortBy)
            || !Enum.IsDefined(parsedSortBy)
            || !string.Equals(sortBy, parsedSortBy.ToString(), StringComparison.OrdinalIgnoreCase)
            || !Enum.TryParse<SortOrder>(sortOrder, true, out var parsedSortOrder)
            || !Enum.IsDefined(parsedSortOrder)
            || !string.Equals(sortOrder, parsedSortOrder.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge payment query",
                detail: "SortBy must be PaymentDate or Amount, and SortOrder must be Ascending or Descending.");
        }

        PledgePaymentQuery query;
        try
        {
            query = new PledgePaymentQuery(page, pageSize, parsedSortBy, parsedSortOrder);
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge payment query",
                detail: exception.Message);
        }

        try
        {
            var payments = await pledgePaymentService.GetByPledgeAsync(
                new PledgeId(pledgeId), query, cancellationToken);
            return Results.Ok(new PledgePaymentPageResponse
            {
                Items = payments.Items.Select(ToPledgePaymentResponse).ToArray(),
                Page = payments.Page,
                PageSize = payments.PageSize,
                TotalCount = payments.TotalCount,
                TotalPages = payments.TotalPages
            });
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge payment query",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "An unexpected error occurred while retrieving payments for pledge {PledgeId}.",
                pledgeId);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledge payments could not be retrieved.");
        }
    }

    private static async Task<IResult> UpdatePledgePaymentAsync(
        Guid id,
        UpdatePledgePaymentRequest request,
        IPledgePaymentService pledgePaymentService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge payment ID",
                detail: "Pledge payment ID cannot be empty.");
        }

        if (request.PledgeId is not { } pledgeId
            || request.Amount is not { } amount
            || request.PaymentDate is not { } paymentDate
            || request.Active is not { } active)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge payment update",
                detail: "PledgeId, Amount, PaymentDate, and Active are required.");
        }

        try
        {
            var pledgePayment = await pledgePaymentService.UpdateAsync(
                new PledgePaymentId(id), new PledgeId(pledgeId), amount, paymentDate,
                request.Comment, active, cancellationToken);
            return pledgePayment is null
                ? Results.NotFound()
                : Results.Ok(ToPledgePaymentResponse(pledgePayment));
        }
            catch (RelationshipConflictException exception)
            {
                return ProblemDetailsHelpers.RelationshipConflict(exception);
            }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge payment update",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception, "An unexpected error occurred while updating pledge payment {PledgePaymentId}.", id);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledge payment could not be updated.");
        }
    }

    private static async Task<IResult> GetPledgePaymentByIdAsync(
        Guid id,
        IPledgePaymentService pledgePaymentService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge payment ID",
                detail: "Pledge payment ID cannot be empty.");
        }

        try
        {
            var pledgePayment = await pledgePaymentService.GetByIdAsync(
                new PledgePaymentId(id), cancellationToken);
            return pledgePayment is null
                ? Results.NotFound()
                : Results.Ok(ToPledgePaymentResponse(pledgePayment));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge payment ID",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception, "An unexpected error occurred while retrieving pledge payment {PledgePaymentId}.", id);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledge payment could not be retrieved.");
        }
    }

    private static async Task<IResult> RemovePledgePaymentAsync(
        Guid id,
        IPledgePaymentService pledgePaymentService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid pledge payment ID",
                detail: "Pledge payment ID cannot be empty.");
        }

        try
        {
            await pledgePaymentService.RemoveAsync(new PledgePaymentId(id), cancellationToken);
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
                title: "Invalid pledge payment ID",
                detail: ProblemDetailsHelpers.GetClientSafeMessage(exception));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception, "An unexpected error occurred while removing pledge payment {PledgePaymentId}.", id);
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Pledge payment could not be removed.");
        }
    }

    private static PledgePaymentResponse ToPledgePaymentResponse(PledgePayment pledgePayment)
    {
        return new PledgePaymentResponse
        {
            Id = pledgePayment.Id.Value,
            PledgeId = pledgePayment.PledgeId.Value,
            Amount = pledgePayment.Amount,
            PaymentDate = pledgePayment.PaymentDate,
            Comment = pledgePayment.Comment,
            Active = pledgePayment.Active,
            CreatedAt = pledgePayment.CreatedAt,
            UpdatedAt = pledgePayment.UpdatedAt
        };
    }
}
