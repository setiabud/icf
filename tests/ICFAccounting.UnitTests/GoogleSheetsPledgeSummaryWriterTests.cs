using ICFAccounting.Application.Services;
using ICFAccounting.ReminderFunction.GoogleSheets;

namespace ICFAccounting.UnitTests;

public sealed class GoogleSheetsPledgeSummaryWriterTests
{
    [Fact]
    public void CreateRows_returns_header_and_one_row_per_summary()
    {
        PledgerPledgeSummary[] summaries =
        [
            new("Ada Lovelace", "ada@example.com", "<h1>Summary</h1>")
        ];

        var rows = GoogleSheetsPledgeSummaryWriterService.CreateRows(summaries);

        Assert.Equal(2, rows.Count);
        Assert.Equal(["Pledger Name", "Email", "HTML Summary"], rows[0]);
        Assert.Equal(["Ada Lovelace", "ada@example.com", "<h1>Summary</h1>"], rows[1]);
    }

    [Fact]
    public void CreateRows_returns_only_header_for_empty_summaries()
    {
        var rows = GoogleSheetsPledgeSummaryWriterService.CreateRows([]);

        Assert.Single(rows);
        Assert.Equal(["Pledger Name", "Email", "HTML Summary"], rows[0]);
    }
}
