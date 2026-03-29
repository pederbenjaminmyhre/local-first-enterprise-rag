using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnterpriseRag.Core.Configuration;
using EnterpriseRag.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseRag.Infrastructure.Ollama;

public class OllamaEmbeddingClient : IEmbeddingClient
{
    private readonly HttpClient _http;
    private readonly OllamaSettings _settings;
    private readonly ILogger<OllamaEmbeddingClient> _logger;

    public OllamaEmbeddingClient(
        HttpClient http,
        IOptions<OllamaSettings> settings,
        ILogger<OllamaEmbeddingClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var request = new EmbedRequest
        {
            Model = _settings.EmbeddingModel,
            Input = text
        };

        _logger.LogDebug("Requesting embedding for {Length}-char text using {Model}",
            text.Length, _settings.EmbeddingModel);

        var response = await _http.PostAsJsonAsync("/api/embed", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbedResponse>(ct)
            ?? throw new InvalidOperationException("Ollama returned null embedding response.");

        if (result.Embeddings is null || result.Embeddings.Length == 0)
            throw new InvalidOperationException("Ollama returned empty embeddings array.");

        _logger.LogDebug("Received {Dims}-dimensional embedding", result.Embeddings[0].Length);
        return result.Embeddings[0];
    }

    private class EmbedRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;
    }

    private class EmbedResponse
    {
        [JsonPropertyName("embeddings")]
        public float[][]? Embeddings { get; set; }
    }
}
