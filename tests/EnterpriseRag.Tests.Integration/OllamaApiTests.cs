using EnterpriseRag.Core.Configuration;
using EnterpriseRag.Infrastructure.Ollama;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseRag.Tests.Integration;

[Trait("Category", "Integration")]
[TestCaseOrderer(
    "EnterpriseRag.Tests.Integration.PriorityOrderer",
    "EnterpriseRag.Tests.Integration")]
public class OllamaApiTests
{
    private static readonly OllamaSettings Settings = new()
    {
        BaseUrl = "http://localhost:11434",
        EmbeddingModel = "mxbai-embed-large",
        GenerationModel = "llama3.2"
    };

    [Fact, TestPriority(1)]
    public async Task Embedding_Returns1024Dims_AndDifferentiatesTexts()
    {
        using var http = new HttpClient { BaseAddress = new Uri(Settings.BaseUrl), Timeout = TimeSpan.FromMinutes(3) };
        var client = new OllamaEmbeddingClient(http, Options.Create(Settings), NullLogger<OllamaEmbeddingClient>.Instance);

        var embedding1 = await client.GetEmbeddingAsync("A lightweight mountain bike for trail riding");
        var embedding2 = await client.GetEmbeddingAsync("financial quarterly report");

        embedding1.Should().HaveCount(1024);
        embedding1.Should().Contain(v => v != 0f);
        embedding1.Should().NotBeEquivalentTo(embedding2);
    }

    [Fact, TestPriority(2)]
    public async Task Generation_ReturnsNonEmptyResponse()
    {
        using var http = new HttpClient { BaseAddress = new Uri(Settings.BaseUrl), Timeout = TimeSpan.FromMinutes(2) };
        var client = new OllamaGenerationClient(http, Options.Create(Settings), NullLogger<OllamaGenerationClient>.Instance);

        var result = await client.GenerateAsync("Say 'hello' and nothing else.");

        result.Should().NotBeNullOrWhiteSpace();
        result.ToLowerInvariant().Should().Contain("hello");
    }

    [Fact, TestPriority(3)]
    public async Task Generation_StreamingYieldsMultipleTokens()
    {
        using var http = new HttpClient { BaseAddress = new Uri(Settings.BaseUrl), Timeout = TimeSpan.FromMinutes(2) };
        var client = new OllamaGenerationClient(http, Options.Create(Settings), NullLogger<OllamaGenerationClient>.Instance);

        var tokens = new List<string>();
        await foreach (var token in client.GenerateStreamAsync("Count from 1 to 5."))
        {
            tokens.Add(token);
            if (tokens.Count >= 10) break;
        }

        tokens.Should().NotBeEmpty();
        tokens.Count.Should().BeGreaterThan(1);
    }
}
