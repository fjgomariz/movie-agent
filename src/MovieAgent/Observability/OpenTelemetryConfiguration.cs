using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Azure.Monitor.OpenTelemetry.AspNetCore;

namespace MovieAgent.Observability;

public static class OpenTelemetryConfiguration
{
    public static IServiceCollection AddMovieAgentOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceName = configuration["MOVIE_AGENT_NAME"] ?? "MovieFinder";
        var serviceVersion = "1.0.0";

        // Configure resource attributes
        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production",
                ["service.namespace"] = "MovieAgent"
            });

        var appInsightsConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        
        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            // Use Azure Monitor OpenTelemetry distro when App Insights is configured
            services.AddOpenTelemetry()
                .UseAzureMonitor(options =>
                {
                    options.ConnectionString = appInsightsConnectionString;
                })
                .WithTracing(tracing => tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    }))
                .WithMetrics(metrics => metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation());
        }
        else
        {
            // Fallback configuration for local development without App Insights
            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(serviceName, serviceVersion))
                .WithTracing(tracing => tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    }))
                .WithMetrics(metrics => metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation());
        }

        return services;
    }
}
