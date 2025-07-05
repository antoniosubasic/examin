using backend.Services;

namespace backend.Routes;

public static class SchoolRoutes
{
    public static void MapSchoolRoutes(this WebApplication app)
    {
        var schoolGroup = app.MapGroup("/api/schools")
            .WithTags("Schools")
            .WithOpenApi();

        schoolGroup.MapGet("/search", async (string query, WebuntisService webuntisService) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return Results.BadRequest(new { error = "search query is required" });
                }

                var schools = await webuntisService.SearchSchoolsAsync(query);
                return Results.Ok(schools);
            }
            catch (Exception ex)
            {
                return ex.Message == "too many results"
                    ? Results.BadRequest(new { error = ex.Message })
                    : Results.Problem($"error searching schools: {ex.Message}");
            }
        })
        .WithName("SearchSchools")
        .WithSummary("Search for schools")
        .WithDescription("Search for WebUntis schools by name or location");
    }
}
