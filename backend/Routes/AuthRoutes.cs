using backend.Services;
using backend.Models;

namespace backend.Routes;

public static class AuthRoutes
{
    public static void MapAuthRoutes(this WebApplication app)
    {
        var authGroup = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        authGroup.MapPost("/login", async (LoginRequest request, WebuntisService webuntisService) =>
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
                return result.Success ? Results.Ok(result) : Results.BadRequest(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"error during login: {ex.Message}");
            }
        })
        .WithName("Login")
        .WithSummary("Login to WebUntis")
        .WithDescription("Authenticate with WebUntis using school credentials");

        authGroup.MapPost("/logout", (string sessionId, WebuntisService webuntisService) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    return Results.BadRequest(new { error = "session ID is required" });
                }

                webuntisService.Logout(sessionId);
                return Results.Ok(new { message = "logged out successfully" });
            }
            catch (Exception ex)
            {
                return Results.Problem($"error during logout: {ex.Message}");
            }
        })
        .WithName("Logout")
        .WithSummary("Logout from WebUntis")
        .WithDescription("Invalidate the current session");
    }
}
