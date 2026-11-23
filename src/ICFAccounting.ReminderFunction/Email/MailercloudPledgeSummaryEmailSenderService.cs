using System.Text;
using System.Text.Json;
using ICFAccounting.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ICFAccounting.ReminderFunction.Email;

public sealed class MailercloudPledgeSummaryEmailSenderService(
    HttpClient httpClient,
    IOptions<MailercloudEmailOptions> options,
    ILogger<MailercloudPledgeSummaryEmailSenderService> logger
) : IPledgeSummaryEmailSenderService
{
    private readonly HttpClient _httpClient =
        httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly MailercloudEmailOptions _options =
        options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<MailercloudPledgeSummaryEmailSenderService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task SendAsync(
        PledgerPledgeSummary summary,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(summary);

        using var request = new HttpRequestMessage(HttpMethod.Post, "email");
        request.Headers.TryAddWithoutValidation("Authorization", _options.ApiKey);

        var payload = new
        {
            version = "1.0",
            email = new
            {
                from = _options.SenderEmail,
                fromName = _options.SenderName,
                subject = _options.Subject,
                html = summary.Html,
                recipients = new
                {
                    to = new[]
                    {
                        new
                        {
                            name = summary.PledgerName,
                            email = summary.Email
                        }
                    }
                }
            },
            metadata = new
            {
                campaignType = "TRANSACTIONAL",
                messageId = Guid.NewGuid().ToString()
            }
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "Pledge summary email accepted by Mailercloud for {PledgerName}.",
                summary.PledgerName
            );
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "Mailercloud rejected a pledge summary email with status {StatusCode}. Response: {ResponseBody}",
            (int)response.StatusCode,
            responseBody
        );
        throw new InvalidOperationException(
            $"Mailercloud rejected the email with HTTP status {(int)response.StatusCode}."
        );
    }
}
