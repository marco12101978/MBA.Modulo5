using BFF.API.Configuration;
using BFF.API.Extensions;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

[ExcludeFromCodeCoverage]
internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics =>
        {
            metrics
                .SetResourceBuilder(
                    OpenTelemetry.Resources.ResourceBuilder.CreateDefault()
                        .AddService(serviceName: builder.Environment.ApplicationName))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter();
        });

        builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

        builder.AddApiConfiguration();

        builder.Services.AddJsonConfiguration();

        builder.Services.AddAuthorization();

        builder.Services.AddHealthChecks();

        var urls = builder.Configuration["Urls"];
        if (!string.IsNullOrEmpty(urls))
        {
            builder.WebHost.UseUrls(urls);
        }

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerConfiguration();
        }

        app.UseCors("AllowedOrigins");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.MapPrometheusScrapingEndpoint("/metrics");

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready")
        });


        app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", DateTime = DateTime.UtcNow }))
            .WithName("HealthCheck")
            .WithOpenApi();

        app.Run();
    }
}
