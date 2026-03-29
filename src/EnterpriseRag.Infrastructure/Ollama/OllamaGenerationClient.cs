using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EnterpriseRag.Core.Configuration;
using EnterpriseRag.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseRag.Infrastructure.Ollama;

public class OllamaGenerationClient : IGenerationClient
{
    private readonly HttpClient _http;
    private readonly OllamaSettings _settings;
    private readonly ILogger<OllamaGenerationClient> _logger;

    public OllamaGenerationClient(
        HttpClient http,
        IOptions<OllamaSettings> settings,
        ILogger<OllamaGenerationClient> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken ct = default)
    {
        var request = new GenerateRequest
        {
            Model = _settings.GenerationModel,
            Prompt = prompt,
            Stream = false
        };

        _logger.LogDebug("Generating response using {Model} (non-streaming)", _settings.GenerationModel);

        var response = await _http.PostAsJsonAsync("/api/generate", request, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GenerateResponse>(ct)
            ?? throw new InvalidOperationException("Ollama returned null generation response.");

        _logger.LogDebug("Generation complete: {Length} chars", result.Response?.Length ?? 0);
        return result.Response ?? string.Empty;
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var request = new GenerateRequest
        {
            Model = _settings.GenerationModel,
            Prompt = prompt,
            Stream = true
        };

        _logger.LogDebug("Generating response using {Model} (streaming)", _settings.GenerationModel);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/generate")
        {
            Content = JsonContent.Create(request)
        };

        var response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var chunk = JsonSerializer.Deserialize<GenerateResponse>(line);
            if (chunk?.Response is not null)
                yield return chunk.Response;

            if (chunk?.Done == true)
                yield break;
        }
    }

    private class GenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private class GenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }
}
