using System.Net;
using System.Text;
using System.Text.Json;
using ICFAccounting.Application.Services;
using ICFAccounting.ReminderFunction.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ICFAccounting.UnitTests.Email;

public sealed class MailercloudPledgeSummaryEmailSenderTests
{
    [Fact]
    public async Task SendAsync_uses_mailercloud_payload()
    {
        var handler = new RecordingHttpMessageHandler(HttpStatusCode.OK);
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://email-api.mailercloud.com/")
        };
        var options = Options.Create(new MailercloudEmailOptions
        {
            ApiKey = "test-api-key",
            SenderEmail = "pledges@example.org",
            SenderName = "Pledge Office",
            Subject = "ICF Pledge Summary"
        });
        var service = new MailercloudPledgeSummaryEmailSenderService(
            client,
            options,
            NullLogger<MailercloudPledgeSummaryEmailSenderService>.Instance
        );
        var summary = new PledgerPledgeSummary(
            "Ada Lovelace",
            "ada@example.com",
            "<h1>Pledge Summary</h1>"
        );

        await service.SendAsync(summary);

        Assert.NotNull(handler.RequestBody);
        Assert.Equal("/email", handler.RequestUri);
        using var document = JsonDocument.Parse(handler.RequestBody);
        var root = document.RootElement;
        Assert.Equal("1.0", root.GetProperty("version").GetString());
        Assert.Equal("ICF Pledge Summary", root.GetProperty("email").GetProperty("subject").GetString());
        Assert.Equal("pledges@example.org", root.GetProperty("email").GetProperty("from").GetString());
        Assert.Equal("Pledge Office", root.GetProperty("email").GetProperty("fromName").GetString());
        Assert.Equal("<h1>Pledge Summary</h1>", root.GetProperty("email").GetProperty("html").GetString());
        Assert.Equal("Ada Lovelace", root.GetProperty("email").GetProperty("recipients").GetProperty("to")[0].GetProperty("name").GetString());
        Assert.Equal("ada@example.com", root.GetProperty("email").GetProperty("recipients").GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("TRANSACTIONAL", root.GetProperty("metadata").GetProperty("campaignType").GetString());
        Assert.True(Guid.TryParse(root.GetProperty("metadata").GetProperty("messageId").GetString(), out _));
        Assert.Equal("test-api-key", handler.Authorization);
    }

    [Fact]
    public async Task SendAsync_throws_for_mailercloud_failure()
    {
        var handler = new RecordingHttpMessageHandler(
            HttpStatusCode.BadRequest,
            "{\"statusCode\":9999,\"message\":\"invalid request\"}"
        );
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://email-api.mailercloud.com/")
        };
        var options = Options.Create(new MailercloudEmailOptions
        {
            ApiKey = "test-api-key",
            SenderEmail = "pledges@example.org",
            SenderName = "Pledge Office",
            Subject = "ICF Pledge Summary"
        });
        var service = new MailercloudPledgeSummaryEmailSenderService(
            client,
            options,
            NullLogger<MailercloudPledgeSummaryEmailSenderService>.Instance
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
        public string? RequestUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            Authorization = request.Headers.Authorization?.ToString();
            RequestUri = request.RequestUri?.AbsolutePath;
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
