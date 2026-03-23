using melanki.trippeltrumf.service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var environmentName =
    Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
    EnvironmentName = environmentName
});
builder.Services.AddTrippelTrumfService(builder.Configuration);

var host = builder.Build();
var environment = host.Services.GetRequiredService<IHostEnvironment>();
var configuration = host.Services.GetRequiredService<IConfiguration>();
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
logger.LogDebug(
    "Service starting. environment={Environment}, applicationName={ApplicationName}, contentRoot={ContentRoot}",
    environment.EnvironmentName,
    environment.ApplicationName,
    environment.ContentRootPath);
logger.LogInformation(
    "Configuration loaded. environment={Environment}, contentRoot={ContentRoot}, webhookConfigured={WebhookConfigured}",
    environment.EnvironmentName,
    environment.ContentRootPath,
    !string.IsNullOrWhiteSpace(configuration["TrippelTrumfService:SlackWorkflowWebhookUrl"]));

await host.RunAsync();
