using melanki.trippeltrumf.service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

var environmentName =
    Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
    EnvironmentName = environmentName
});
builder.Logging.ClearProviders();
builder.Services.AddSerilog((services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ApplicationName", builder.Environment.ApplicationName)
        .Enrich.WithProperty("EnvironmentName", builder.Environment.EnvironmentName);
});
builder.Services.AddTrippelTrumfService(builder.Configuration);

try
{
    var host = builder.Build();
    var environment = host.Services.GetRequiredService<IHostEnvironment>();
    var configuration = host.Services.GetRequiredService<IConfiguration>();
    var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogDebug(
        "Service starting. Environment {EnvironmentName}, Application {ApplicationName}, ContentRoot {ContentRootPath}",
        environment.EnvironmentName,
        environment.ApplicationName,
        environment.ContentRootPath);
    logger.LogInformation(
        "Configuration loaded. Environment {EnvironmentName}, ContentRoot {ContentRootPath}, WebhookConfigured {WebhookConfigured}",
        environment.EnvironmentName,
        environment.ContentRootPath,
        !string.IsNullOrWhiteSpace(configuration["TrippelTrumfService:SlackWorkflowWebhookUrl"]));

    await host.RunAsync();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Service terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync();
}
