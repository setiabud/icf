namespace ICFAccounting.ReminderFunction.GoogleSheets;

public sealed class GoogleSheetsOptions
{
    public const string SectionName = "GoogleSheets";

    public string SpreadsheetId { get; set; } = string.Empty;
    public string Range { get; set; } = "Monthly Summary!A:C";
    public string CredentialsFile { get; set; } = string.Empty;
}
