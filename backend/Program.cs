using backend.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplicationServices()
    .AddApplicationCors();

var app = builder.Build();

app.ConfigurePipeline()
   .MapApplicationRoutes();

app.Run();
