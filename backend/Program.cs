using backend.Services;
using backend.Models;
using System.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<WebuntisService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();

app.MapHealthChecks("/health");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapGet("/api/schools/search", async (string query, WebuntisService webuntisService) =>
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
        return Results.Problem($"error searching schools: {ex.Message}");
    }
});

app.MapPost("/api/auth/login", async (LoginRequest request, WebuntisService webuntisService) =>
{
    try
    {
        if (
            string.IsNullOrWhiteSpace(request.School)
            || string.IsNullOrWhiteSpace(request.Username)
            || string.IsNullOrWhiteSpace(request.Password)
        )
        {
            return Results.BadRequest(new { error = "school, username, and password are required" });
        }

        var result = await webuntisService.LoginAsync(request.School, request.Username, request.Password);

        if (result.Success)
        {
            return Results.Ok(result);
        }
        else
        {
            return Results.BadRequest(result);
        }
    }
    catch (Exception ex)
    {
        return Results.Problem($"error during login: {ex.Message}");
    }
});

app.MapPost("/api/auth/logout", (string sessionId, WebuntisService webuntisService) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Results.BadRequest(new { error = "session id is required" });
        }

        webuntisService.Logout(sessionId);
        return Results.Ok(new { message = "logged out successfully" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"error during logout: {ex.Message}");
    }
});

app.MapGet("/api/exams", async (string sessionId, DateOnly startDate, DateOnly endDate, WebuntisService webuntisService) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Results.BadRequest(new { error = "session id is required" });
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
});

app.MapPost("/api/exams/range", async (ExamRequest request, WebuntisService webuntisService) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return Results.BadRequest(new { error = "session id is required" });
        }

        var exams = await webuntisService.GetExamsAsync(request.SessionId, request.StartDate, request.EndDate);
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
});

app.Run();
