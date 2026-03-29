using System.Net;
using System.Text.Json;
using EnterpriseRag.Core.Configuration;
using EnterpriseRag.Infrastructure.Ollama;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace EnterpriseRag.Tests.Unit;

public class OllamaEmbeddingClientTests
{
    private readonly OllamaSettings _settings = new()
    {
        BaseUrl = "http://localhost:11434",
        EmbeddingModel = "mxbai-embed-large"
    };

    private OllamaEmbeddingClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(_settings.BaseUrl) };
        return new OllamaEmbeddingClient(
            httpClient,
            Options.Create(_settings),
            NullLogger<OllamaEmbeddingClient>.Instance);
    }

    [Fact]
    public async Task GetEmbeddingAsync_ReturnsFloatArray_WhenOllamaRespondsSuccessfully()
    {
        // Arrange
        var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
        var responseBody = JsonSerializer.Serialize(new
        {
            embeddings = new[] { expectedEmbedding }
        });

        var handler = new FakeHttpHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handler);

        // Act
        var result = await client.GetEmbeddingAsync("test text");

        // Assert
        result.Should().BeEquivalentTo(expectedEmbedding);
    }

    [Fact]
    public async Task GetEmbeddingAsync_SendsCorrectModelInRequest()
    {
        // Arrange
        var responseBody = JsonSerializer.Serialize(new
        {
            embeddings = new[] { new float[] { 0.1f } }
        });

        var handler = new FakeHttpHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handler);

        // Act
        await client.GetEmbeddingAsync("test text");

        // Assert
        var requestBody = handler.CapturedRequestBody;
        requestBody.Should().Contain("\"model\":\"mxbai-embed-large\"");
    }

    [Fact]
    public async Task GetEmbeddingAsync_ThrowsOnEmptyEmbeddings()
    {
        // Arrange
        var responseBody = JsonSerializer.Serialize(new { embeddings = Array.Empty<float[]>() });
        var handler = new FakeHttpHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handler);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetEmbeddingAsync("test text"));
    }

    [Fact]
    public async Task GetEmbeddingAsync_ThrowsOnHttpError()
    {
        // Arrange
        var handler = new FakeHttpHandler(HttpStatusCode.InternalServerError, "error");
        var client = CreateClient(handler);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetEmbeddingAsync("test text"));
    }
}

/// <summary>
/// Simple fake HttpMessageHandler for unit testing HTTP clients.
/// </summary>
internal class FakeHttpHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseBody;

    public string? CapturedRequestBody { get; private set; }

    public FakeHttpHandler(HttpStatusCode statusCode, string responseBody)
    {
        _statusCode = statusCode;
        _responseBody = responseBody;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is not null)
            CapturedRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody, System.Text.Encoding.UTF8, "application/json")
        };
    }
}
