using System.Text.Json.Serialization;
using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.API.Endpoints;
using CulinaryBlog.API.Middleware;
using Scalar.AspNetCore;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((doc, context, ct) =>
{
    doc.Info.Title = "Culinary Blog API — Chương 1";
    doc.Info.Version = "v1";
    doc.Info.Description = "CRUD Category và Recipe; CQRS, phân trang, tìm kiếm, sắp xếp và lọc thời gian nấu.";
    return Task.CompletedTask;
}));
var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(o => o.WithTitle("Culinary Blog API").WithTheme(ScalarTheme.Purple));
}
app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
app.MapCategoryEndpoints();
app.MapRecipeEndpoints();
app.Run();
