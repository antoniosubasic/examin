using backend.Services;
using backend.Models;
using System.Security.Authentication;

namespace backend.Routes;

public static class ExamRoutes
{
    public static void MapExamRoutes(this WebApplication app)
    {
        var examGroup = app.MapGroup("/api/exams")
            .WithTags("Exams")
            .WithOpenApi();

        examGroup.MapGet("/", async (string sessionId, DateOnly startDate, DateOnly endDate, WebuntisService webuntisService) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    return Results.BadRequest(new { error = "session ID is required" });
                }

                var exams = await webuntisService.GetExamsAsync(sessionId, startDate, endDate);
                return Results.Ok(exams);
            }
            catch (AuthenticationException)
            {
                return Results.Json(new { error = "invalid or expired session" }, statusCode: 401);
            }
            catch (Exception ex)
            {
                return Results.Problem($"error getting exams: {ex.Message}");
            }
        })
        .WithName("GetExams")
        .WithSummary("Get exams by date range")
        .WithDescription("Retrieve exams for a specific date range using query parameters");

        examGroup.MapPost("/range", async (ExamRequest request, WebuntisService webuntisService) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SessionId))
                {
                    return Results.BadRequest(new { error = "Session ID is required" });
                }

                var exams = await webuntisService.GetExamsAsync(request.SessionId, request.StartDate, request.EndDate);
                return Results.Ok(exams);
            }
            catch (AuthenticationException)
            {
                return Results.Json(new { error = "Invalid or expired session" }, statusCode: 401);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error getting exams: {ex.Message}");
            }
        })
        .WithName("GetExamsByRange")
        .WithSummary("Get exams by date range (POST)")
        .WithDescription("Retrieve exams for a specific date range using request body");
    }
}
