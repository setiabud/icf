using System.Net;
using System.Text;
using System.Text.Json;
using ICFAccounting.Application.Services;
using ICFAccounting.ReminderFunction.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ICFAccounting.UnitTests.Email;

public sealed class SendGridPledgeSummaryEmailSenderTests
{
    [Fact]
    public async Task SendAsync_uses_dynamic_template_payload()
    {
        var handler = new RecordingHttpMessageHandler(HttpStatusCode.Accepted);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.sendgrid.com/v3/")
        };
        var options = Options.Create(new SendGridEmailOptions
        {
            ApiKey = "test-api-key",
            TemplateId = "d-test-template",
            SenderEmail = "pledges@example.org",
            SenderName = "Pledge Office"
        });
        var service = new SendGridPledgeSummaryEmailSenderService(
            client,
            options,
            NullLogger<SendGridPledgeSummaryEmailSenderService>.Instance
        );
        var summary = new PledgerPledgeSummary(
            "Ada Lovelace",
            "ada@example.com",
            "<h1>Pledge Summary</h1>"
        );

        await service.SendAsync(summary);

        Assert.NotNull(handler.RequestBody);
        using var document = JsonDocument.Parse(handler.RequestBody);
        var root = document.RootElement;
        Assert.Equal("d-test-template", root.GetProperty("template_id").GetString());
        Assert.Equal("pledges@example.org", root.GetProperty("from").GetProperty("email").GetString());
        Assert.Equal("Pledge Office", root.GetProperty("from").GetProperty("name").GetString());
        Assert.Equal("ada@example.com", root.GetProperty("personalizations")[0].GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("Ada Lovelace", root.GetProperty("personalizations")[0].GetProperty("dynamic_template_data").GetProperty("pledgerName").GetString());
        Assert.Equal("<h1>Pledge Summary</h1>", root.GetProperty("personalizations")[0].GetProperty("dynamic_template_data").GetProperty("summaryHtml").GetString());
        Assert.Equal("Bearer test-api-key", handler.Authorization);
    }

    [Fact]
    public async Task SendAsync_throws_for_sendgrid_failure()
    {
        var handler = new RecordingHttpMessageHandler(
            HttpStatusCode.BadRequest,
            "{\"errors\":[{\"message\":\"invalid template\"}]}"
        );
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.sendgrid.com/v3/")
        };
        var options = Options.Create(new SendGridEmailOptions
        {
            ApiKey = "test-api-key",
            TemplateId = "d-test-template",
            SenderEmail = "pledges@example.org",
            SenderName = "Pledge Office"
        });
        var service = new SendGridPledgeSummaryEmailSenderService(
            client,
            options,
            NullLogger<SendGridPledgeSummaryEmailSenderService>.Instance
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendAsync(new PledgerPledgeSummary("Ada", "ada@example.com", "<p>Summary</p>"))
        );
    }

    private sealed class RecordingHttpMessageHandler(
        HttpStatusCode statusCode,
        string responseBody = ""
    ) : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }
        public string? Authorization { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            Authorization = request.Headers.Authorization?.ToString();
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
