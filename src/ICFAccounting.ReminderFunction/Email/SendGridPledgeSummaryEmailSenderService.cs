using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ICFAccounting.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ICFAccounting.ReminderFunction.Email;

public sealed class SendGridPledgeSummaryEmailSenderService(
    HttpClient httpClient,
    IOptions<SendGridEmailOptions> options,
    ILogger<SendGridPledgeSummaryEmailSenderService> logger
) : IPledgeSummaryEmailSenderService
{
    private readonly HttpClient _httpClient =
        httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly SendGridEmailOptions _options =
        options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<SendGridPledgeSummaryEmailSenderService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task SendAsync(
        PledgerPledgeSummary summary,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(summary);

        using var request = new HttpRequestMessage(HttpMethod.Post, "mail/send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var payload = new
        {
            personalizations = new[]
            {
                new
                {
                    to = new[] { new { email = summary.Email } },
                    dynamic_template_data = new
                    {
                        pledgerName = summary.PledgerName,
                        summaryHtml = summary.Html
                    }
                }
            },
            from = new
            {
                email = _options.SenderEmail,
                name = _options.SenderName
            },
            template_id = _options.TemplateId
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
                "Pledge summary email accepted by SendGrid for {PledgerName}.",
                summary.PledgerName
            );
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "SendGrid rejected a pledge summary email with status {StatusCode}. Response: {ResponseBody}",
            (int)response.StatusCode,
            responseBody
        );
        throw new InvalidOperationException(
            $"SendGrid rejected the email with HTTP status {(int)response.StatusCode}."
        );
    }
}
