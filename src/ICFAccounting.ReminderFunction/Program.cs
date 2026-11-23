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
    .AddOptions<MailercloudEmailOptions>()
    .Bind(builder.Configuration.GetSection(MailercloudEmailOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "Mailercloud API key is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.SenderEmail), "Mailercloud sender email is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Subject), "Mailercloud subject is required.")
    .ValidateOnStart();
builder.Services.AddHttpClient<MailercloudPledgeSummaryEmailSenderService>(client =>
{
    client.BaseAddress = new Uri("https://email-api.mailercloud.com/");
});
builder.Services.AddScoped<IPledgeSummaryEmailSenderService>(provider =>
    provider.GetRequiredService<MailercloudPledgeSummaryEmailSenderService>()
);

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
