namespace ICFAccounting.ReminderFunction.Email;

public sealed class MailercloudEmailOptions
{
    public const string SectionName = "Mailercloud";

    public string ApiKey { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Subject { get; set; } = "ICF Pledge Summary";
}
