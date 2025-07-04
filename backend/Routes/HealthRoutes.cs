namespace backend.Routes;

public static class HealthRoutes
{
    public static void MapHealthRoutes(this WebApplication app)
    {
        var healthGroup = app.MapGroup("/health")
            .WithTags("Health")
            .WithOpenApi();

        app.MapHealthChecks("/health");

        healthGroup.MapGet("/status", () =>
            Results.Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            }))
            .WithName("HealthStatus")
            .WithSummary("Get detailed health status")
            .WithDescription("Returns detailed health information including timestamp and environment");
    }
}
