using backend.Routes;

namespace backend.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseHttpsRedirection();
        app.UseCors();

        return app;
    }

    public static WebApplication MapApplicationRoutes(this WebApplication app)
    {
        app.MapHealthRoutes();
        app.MapSchoolRoutes();
        app.MapAuthRoutes();
        app.MapExamRoutes();

        return app;
    }
}
