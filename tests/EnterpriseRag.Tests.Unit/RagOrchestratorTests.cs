using EnterpriseRag.Core.Interfaces;
using EnterpriseRag.Core.Models;
using EnterpriseRag.Infrastructure.Rag;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EnterpriseRag.Tests.Unit;

public class RagOrchestratorTests
{
    private readonly Mock<IEmbeddingClient> _embeddingMock = new();
    private readonly Mock<ISearchService> _searchMock = new();
    private readonly Mock<IGenerationClient> _generationMock = new();

    private RagOrchestrator CreateOrchestrator() => new(
        _embeddingMock.Object,
        _searchMock.Object,
        _generationMock.Object,
        NullLogger<RagOrchestrator>.Instance);

    [Fact]
    public async Task AskAsync_ExecutesFullPipeline_EmbedSearchGenerate()
    {
        // Arrange
        var fakeVector = new float[] { 0.1f, 0.2f, 0.3f };
        var fakeResults = new List<SearchResult>
        {
            new()
            {
                ProductKey = 1,
                EnglishProductName = "Mountain Bike",
                EnglishDescription = "A great bike",
                ProductSubcategoryName = "Bikes",
                Color = "Black",
                SimilarityScore = 0.95
            }
        };

        _embeddingMock
            .Setup(e => e.GetEmbeddingAsync("What bikes do you have?", It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeVector);

        _searchMock
            .Setup(s => s.HybridSearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeResults);

        _generationMock
            .Setup(g => g.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("We have a great Mountain Bike in black.");

        var orchestrator = CreateOrchestrator();

        // Act
        var response = await orchestrator.AskAsync("What bikes do you have?");

        // Assert
        response.Answer.Should().Contain("Mountain Bike");
        response.Sources.Should().HaveCount(1);
        response.Sources[0].EnglishProductName.Should().Be("Mountain Bike");
        response.ElapsedTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task AskAsync_PassesEmbeddingToSearchRequest()
    {
        // Arrange
        var fakeVector = new float[] { 1.0f, 2.0f };
        SearchRequest? capturedRequest = null;

        _embeddingMock
            .Setup(e => e.GetEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeVector);

        _searchMock
            .Setup(s => s.HybridSearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SearchRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new List<SearchResult>());

        _generationMock
            .Setup(g => g.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("No results found.");

        var orchestrator = CreateOrchestrator();

        // Act
        await orchestrator.AskAsync("test", category: "Bikes", color: "Red");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.QueryVector.Should().BeEquivalentTo(fakeVector);
        capturedRequest.Category.Should().Be("Bikes");
        capturedRequest.Color.Should().Be("Red");
    }

    [Fact]
    public async Task AskAsync_IncludesProductContextInPrompt()
    {
        // Arrange
        string? capturedPrompt = null;

        _embeddingMock
            .Setup(e => e.GetEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f });

        _searchMock
            .Setup(s => s.HybridSearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SearchResult>
            {
                new()
                {
                    ProductKey = 42,
                    EnglishProductName = "Road-150 Red",
                    EnglishDescription = "Top-of-the-line road bike",
                    ProductSubcategoryName = "Road Bikes",
                    Color = "Red",
                    SimilarityScore = 0.88
                }
            });

        _generationMock
            .Setup(g => g.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((prompt, _) => capturedPrompt = prompt)
            .ReturnsAsync("answer");

        var orchestrator = CreateOrchestrator();

        // Act
        await orchestrator.AskAsync("best road bike?");

        // Assert
        capturedPrompt.Should().NotBeNull();
        capturedPrompt.Should().Contain("Road-150 Red");
        capturedPrompt.Should().Contain("Top-of-the-line road bike");
        capturedPrompt.Should().Contain("best road bike?");
        capturedPrompt.Should().Contain("0.880");
    }

    [Fact]
    public async Task AskAsync_PassesNullFilters_WhenNotProvided()
    {
        // Arrange
        SearchRequest? capturedRequest = null;

        _embeddingMock
            .Setup(e => e.GetEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f });

        _searchMock
            .Setup(s => s.HybridSearchAsync(It.IsAny<SearchRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SearchRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new List<SearchResult>());

        _generationMock
            .Setup(g => g.GenerateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("answer");

        var orchestrator = CreateOrchestrator();

        // Act
        await orchestrator.AskAsync("anything");

        // Assert
        capturedRequest!.Category.Should().BeNull();
        capturedRequest.Color.Should().BeNull();
    }
}
