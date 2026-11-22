using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using ICFAccounting.Application.Services;
using ICFAccounting.Data.Extensions;
using ICFAccounting.ReminderFunction.Email;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddScoped<IPledgeSummaryService, PledgeSummaryService>();
builder.Services
    .AddOptions<SendGridEmailOptions>()
    .Bind(builder.Configuration.GetSection(SendGridEmailOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "SendGrid API key is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.TemplateId), "SendGrid template ID is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.SenderEmail), "SendGrid sender email is required.")
    .ValidateOnStart();
builder.Services.AddHttpClient<SendGridPledgeSummaryEmailSenderService>(client =>
{
    client.BaseAddress = new Uri("https://api.sendgrid.com/v3/");
});
builder.Services.AddScoped<IPledgeSummaryEmailSenderService>(provider =>
    provider.GetRequiredService<SendGridPledgeSummaryEmailSenderService>()
);

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
