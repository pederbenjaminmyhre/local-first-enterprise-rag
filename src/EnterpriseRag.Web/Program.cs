using EnterpriseRag.Core.Interfaces;
using EnterpriseRag.Infrastructure;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// POST /api/ask — RAG query endpoint
app.MapPost("/api/ask", async (AskRequest request, IRagOrchestrator orchestrator, CancellationToken ct) =>
{
    var response = await orchestrator.AskAsync(request.Question, request.Category, request.Color, ct);
    return Results.Ok(new
    {
        response.Answer,
        Sources = response.Sources.Select(s => new
        {
            s.ProductKey,
            s.EnglishProductName,
            s.EnglishDescription,
            s.ProductSubcategoryName,
            s.Color,
            s.SimilarityScore
        }),
        ElapsedMs = response.ElapsedTime.TotalMilliseconds
    });
});

// GET /api/filters — distinct categories and colors
app.MapGet("/api/filters", async (IProductRepository repo, CancellationToken ct) =>
{
    var categories = await repo.GetDistinctCategoriesAsync(ct);
    var colors = await repo.GetDistinctColorsAsync(ct);
    return Results.Ok(new { Categories = categories, Colors = colors });
});

// GET /api/health — service health check
app.MapGet("/api/health", async (IConfiguration config) =>
{
    var checks = new Dictionary<string, string>();

    // Check SQL Server
    try
    {
        var connStr = config.GetSection("Database").GetValue<string>("ConnectionString") ?? "";
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        checks["sqlServer"] = "healthy";
    }
    catch (Exception ex)
    {
        checks["sqlServer"] = $"unhealthy: {ex.Message}";
    }

    // Check Ollama
    try
    {
        var ollamaUrl = config.GetSection("Ollama").GetValue<string>("BaseUrl") ?? "http://localhost:11434";
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var resp = await http.GetAsync($"{ollamaUrl}/api/tags");
        checks["ollama"] = resp.IsSuccessStatusCode ? "healthy" : $"unhealthy: {resp.StatusCode}";
    }
    catch (Exception ex)
    {
        checks["ollama"] = $"unhealthy: {ex.Message}";
    }

    var allHealthy = checks.Values.All(v => v == "healthy");
    return allHealthy ? Results.Ok(checks) : Results.Json(checks, statusCode: 503);
});

app.Run();

record AskRequest(string Question, string? Category = null, string? Color = null);
