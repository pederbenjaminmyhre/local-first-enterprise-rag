using System.Net;
using System.Text.Json;
using EnterpriseRag.Core.Configuration;
using EnterpriseRag.Infrastructure.Ollama;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseRag.Tests.Unit;

public class OllamaGenerationClientTests
{
    private readonly OllamaSettings _settings = new()
    {
        BaseUrl = "http://localhost:11434",
        GenerationModel = "llama3.2"
    };

    private OllamaGenerationClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(_settings.BaseUrl) };
        return new OllamaGenerationClient(
            httpClient,
            Options.Create(_settings),
            NullLogger<OllamaGenerationClient>.Instance);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsResponse_WhenSuccessful()
    {
        // Arrange
        var responseBody = JsonSerializer.Serialize(new { response = "Hello world!", done = true });
        var handler = new FakeHttpHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handler);

        // Act
        var result = await client.GenerateAsync("Say hello");

        // Assert
        result.Should().Be("Hello world!");
    }

    [Fact]
    public async Task GenerateAsync_SendsCorrectModel()
    {
        // Arrange
        var responseBody = JsonSerializer.Serialize(new { response = "ok", done = true });
        var handler = new FakeHttpHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handler);

        // Act
        await client.GenerateAsync("test");

        // Assert
        handler.CapturedRequestBody.Should().Contain("\"model\":\"llama3.2\"");
        handler.CapturedRequestBody.Should().Contain("\"stream\":false");
    }

    [Fact]
    public async Task GenerateAsync_ReturnsEmptyString_WhenResponseIsNull()
    {
        // Arrange
        var responseBody = JsonSerializer.Serialize(new { response = (string?)null, done = true });
        var handler = new FakeHttpHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handler);

        // Act
        var result = await client.GenerateAsync("test");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateStreamAsync_YieldsTokens_FromNdjsonStream()
    {
        // Arrange — simulate NDJSON streaming response
        var lines = string.Join("\n",
            JsonSerializer.Serialize(new { response = "Hello", done = false }),
            JsonSerializer.Serialize(new { response = " world", done = false }),
            JsonSerializer.Serialize(new { response = "!", done = true }));

        var handler = new FakeHttpHandler(HttpStatusCode.OK, lines);
        var client = CreateClient(handler);

        // Act
        var tokens = new List<string>();
        await foreach (var token in client.GenerateStreamAsync("Say hello"))
        {
            tokens.Add(token);
        }

        // Assert
        tokens.Should().BeEquivalentTo(new[] { "Hello", " world", "!" });
    }

    [Fact]
    public async Task GenerateAsync_ThrowsOnHttpError()
    {
        // Arrange
        var handler = new FakeHttpHandler(HttpStatusCode.ServiceUnavailable, "down");
        var client = CreateClient(handler);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GenerateAsync("test"));
    }
}
