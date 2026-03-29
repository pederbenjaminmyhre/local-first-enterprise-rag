using System.Diagnostics;
using System.Text;
using EnterpriseRag.Core.Interfaces;
using EnterpriseRag.Core.Models;
using Microsoft.Extensions.Logging;

namespace EnterpriseRag.Infrastructure.Rag;

public class RagOrchestrator : IRagOrchestrator
{
    private readonly IEmbeddingClient _embeddingClient;
    private readonly ISearchService _searchService;
    private readonly IGenerationClient _generationClient;
    private readonly ILogger<RagOrchestrator> _logger;

    public RagOrchestrator(
        IEmbeddingClient embeddingClient,
        ISearchService searchService,
        IGenerationClient generationClient,
        ILogger<RagOrchestrator> logger)
    {
        _embeddingClient = embeddingClient;
        _searchService = searchService;
        _generationClient = generationClient;
        _logger = logger;
    }

    public async Task<RagResponse> AskAsync(
        string question,
        string? category = null,
        string? color = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("RAG pipeline started for question: {Question}", question);

        // Step 1: Convert question to embedding
        var embedding = await _embeddingClient.GetEmbeddingAsync(question, ct);
        _logger.LogDebug("Embedding generated in {Elapsed}ms", stopwatch.ElapsedMilliseconds);

        // Step 2: Hybrid search for relevant products
        var searchRequest = new SearchRequest
        {
            QueryVector = embedding,
            Category = category,
            Color = color
        };
        var results = await _searchService.HybridSearchAsync(searchRequest, ct);
        _logger.LogDebug("Search returned {Count} results in {Elapsed}ms",
            results.Count, stopwatch.ElapsedMilliseconds);

        // Step 3: Build prompt with retrieved context
        var prompt = BuildPrompt(question, results);

        // Step 4: Generate natural language response
        var answer = await _generationClient.GenerateAsync(prompt, ct);

        stopwatch.Stop();
        _logger.LogInformation("RAG pipeline completed in {Elapsed}ms", stopwatch.ElapsedMilliseconds);

        return new RagResponse
        {
            Answer = answer,
            Sources = results,
            ElapsedTime = stopwatch.Elapsed
        };
    }

    private static string BuildPrompt(string question, IReadOnlyList<SearchResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine("""
            You are a knowledgeable product advisor for AdventureWorks. Answer the user's
            question using ONLY the product information provided below. If the information
            is insufficient, say so. Be concise and helpful.
            """);

        sb.AppendLine();
        sb.AppendLine("## Relevant Products");

        foreach (var r in results)
        {
            sb.AppendLine(
                $"- **{r.EnglishProductName}** ({r.ProductSubcategoryName ?? "N/A"}, {r.Color ?? "N/A"}): " +
                $"{r.EnglishDescription ?? "No description"} [Score: {r.SimilarityScore:F3}]");
        }

        sb.AppendLine();
        sb.AppendLine("## User Question");
        sb.AppendLine(question);

        return sb.ToString();
    }
}
