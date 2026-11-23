using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using ICFAccounting.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ICFAccounting.ReminderFunction.GoogleSheets;

public sealed class GoogleSheetsPledgeSummaryWriterService(
    IOptions<GoogleSheetsOptions> options,
    ILogger<GoogleSheetsPledgeSummaryWriterService> logger
) : IPledgeSummarySheetWriterService
{
    private readonly GoogleSheetsOptions _options =
        options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<GoogleSheetsPledgeSummaryWriterService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task WriteAsync(
        IReadOnlyList<PledgerPledgeSummary> summaries,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(summaries);

        var credential = CredentialFactory
            .FromFile<ServiceAccountCredential>(_options.CredentialsFile)
            .ToGoogleCredential()
            .CreateScoped(SheetsService.Scope.Spreadsheets);

        using var service = new SheetsService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "ICF Accounting"
            });

        await service.Spreadsheets.Values
            .Clear(
                new ClearValuesRequest(),
                _options.SpreadsheetId,
                _options.Range
            )
            .ExecuteAsync(cancellationToken);

        var updateRequest = service.Spreadsheets.Values.Update(
            new ValueRange { Values = CreateRows(summaries) },
            _options.SpreadsheetId,
            _options.Range
        );
        updateRequest.ValueInputOption =
            SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

        await updateRequest.ExecuteAsync(cancellationToken);

        _logger.LogInformation(
            "Replaced Google Sheet range {Range} with {SummaryCount} pledge summaries.",
            _options.Range,
            summaries.Count
        );
    }

    public static IList<IList<object>> CreateRows(
        IReadOnlyList<PledgerPledgeSummary> summaries
    )
    {
        ArgumentNullException.ThrowIfNull(summaries);

        var values = new List<IList<object>>
        {
            new List<object> { "Pledger Name", "Email", "HTML Summary" }
        };

        foreach (var summary in summaries)
        {
            values.Add(
                new List<object>
                {
                    summary.PledgerName,
                    summary.Email,
                    summary.Html
                }
            );
        }

        return values;
    }
}
