using melanki.trippeltrumf.service.Features.Scraping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifying = melanki.trippeltrumf.service.Features.Notifying;
using Polling = melanki.trippeltrumf.service.Features.Polling;

namespace melanki.trippeltrumf.service;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTrippelTrumfService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<Notifying.Options>()
            .Bind(configuration.GetSection(Notifying.Options.SectionName))
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.SlackWorkflowWebhookUrl),
                $"{Notifying.Options.SectionName}:SlackWorkflowWebhookUrl must be configured.")
            .ValidateOnStart();

        services.AddSingleton<Scraper>();
        services.AddSingleton<Polling.StateStore>();
        services.AddSingleton<Polling.ChangeFeed>();
        services.AddHttpClient<Notifying.Client>();
        services.AddHostedService<Polling.Worker>();
        services.AddHostedService<Notifying.Worker>();

        return services;
    }
}
