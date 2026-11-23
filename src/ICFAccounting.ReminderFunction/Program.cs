using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using ICFAccounting.Application.Services;
using ICFAccounting.Data.Extensions;
using ICFAccounting.ReminderFunction.GoogleSheets;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddScoped<IPledgeSummaryService, PledgeSummaryService>();
builder.Services
    .AddOptions<GoogleSheetsOptions>()
    .Bind(builder.Configuration.GetSection(GoogleSheetsOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.SpreadsheetId), "Google Sheets spreadsheet ID is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Range), "Google Sheets range is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.CredentialsFile), "Google service-account credentials file is required.")
    .ValidateOnStart();
builder.Services.AddScoped<IPledgeSummarySheetWriterService, GoogleSheetsPledgeSummaryWriterService>();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
