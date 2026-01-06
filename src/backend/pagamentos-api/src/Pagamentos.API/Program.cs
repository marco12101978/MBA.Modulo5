using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Pagamentos.API.Configuration;
using Pagamentos.API.Configuration.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddOpenTelemetry()
    .AddApiConfig()
    .AddCorsConfig()
    .AddSwaggerConfig()
    .AddMessageBusConfiguration(builder.Configuration)
    .AddDbContextConfig()
    .AddJwtConfiguration()
    .AddMapsterConfiguration()
    .AddMediatrConfig()
    .AddDependencyInjectionConfig()
    .AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseSwaggerConfig(apiVersionDescriptionProvider);
    app.UseCors("Development");
}
else
{
    app.UseCors("Production");
}

app.UseHttpsRedirection();

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

app.UseDbMigrationHelper();

app.Run();
