using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnterpriseRag.Core.Configuration;
using EnterpriseRag.Core.Interfaces;
using EnterpriseRag.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseRag.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IRagOrchestrator _ragOrchestrator;
    private readonly IGenerationClient _generationClient;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly ISearchService _searchService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly Dispatcher _dispatcher;
    private CancellationTokenSource? _cts;

    public MainViewModel(
        IRagOrchestrator ragOrchestrator,
        IGenerationClient generationClient,
        IEmbeddingClient embeddingClient,
        ISearchService searchService,
        ILogger<MainViewModel> logger)
    {
        _ragOrchestrator = ragOrchestrator;
        _generationClient = generationClient;
        _embeddingClient = embeddingClient;
        _searchService = searchService;
        _logger = logger;
        _dispatcher = Dispatcher.CurrentDispatcher;

        Categories = new ObservableCollection<string> { "(All Categories)" };
        Colors = new ObservableCollection<string> { "(All Colors)" };
        SelectedCategory = Categories[0];
        SelectedColor = Colors[0];
        Sources = new ObservableCollection<SearchResult>();
    }

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _answer = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string _selectedCategory = string.Empty;

    [ObservableProperty]
    private string _selectedColor = string.Empty;

    [ObservableProperty]
    private string _elapsedTime = string.Empty;

    public ObservableCollection<string> Categories { get; }
    public ObservableCollection<string> Colors { get; }
    public ObservableCollection<SearchResult> Sources { get; }

    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task SearchAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        IsSearching = true;
        Answer = string.Empty;
        Sources.Clear();
        StatusMessage = "Generating embedding...";
        ElapsedTime = string.Empty;

        try
        {
            var category = SelectedCategory == "(All Categories)" ? null : SelectedCategory;
            var color = SelectedColor == "(All Colors)" ? null : SelectedColor;

            // Use streaming for real-time response display
            StatusMessage = "Generating embedding...";
            var embedding = await _embeddingClient.GetEmbeddingAsync(SearchQuery, ct);

            StatusMessage = "Searching products...";
            var searchRequest = new SearchRequest
            {
                QueryVector = embedding,
                Category = category,
                Color = color
            };
            var results = await _searchService.HybridSearchAsync(searchRequest, ct);

            _dispatcher.Invoke(() =>
            {
                foreach (var result in results)
                    Sources.Add(result);
            });

            StatusMessage = "Generating answer...";
            var prompt = BuildPrompt(SearchQuery, results);

            // Stream the answer token by token
            var startTime = DateTime.UtcNow;
            await foreach (var token in _generationClient.GenerateStreamAsync(prompt, ct))
            {
                _dispatcher.Invoke(() => Answer += token);
            }

            var elapsed = DateTime.UtcNow - startTime;
            ElapsedTime = $"Retrieved {results.Count} products | Generated in {elapsed.TotalSeconds:F1}s";
            StatusMessage = "Done";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Search cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed");
            StatusMessage = $"Error: {ex.Message}";
            Answer = $"An error occurred: {ex.Message}\n\nMake sure Ollama and SQL Server are running.";
        }
        finally
        {
            IsSearching = false;
        }
    }

    private bool CanSearch() => !string.IsNullOrWhiteSpace(SearchQuery) && !IsSearching;

    partial void OnSearchQueryChanged(string value) => SearchCommand.NotifyCanExecuteChanged();
    partial void OnIsSearchingChanged(bool value) => SearchCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    private static string BuildPrompt(string question, IReadOnlyList<SearchResult> results)
    {
        var lines = new List<string>
        {
            """
            You are a knowledgeable product advisor for AdventureWorks. Answer the user's
            question using ONLY the product information provided below. If the information
            is insufficient, say so. Be concise and helpful.
            """,
            "",
            "## Relevant Products"
        };

        foreach (var r in results)
        {
            lines.Add(
                $"- **{r.EnglishProductName}** ({r.ProductSubcategoryName ?? "N/A"}, {r.Color ?? "N/A"}): " +
                $"{r.EnglishDescription ?? "No description"} [Score: {r.SimilarityScore:F3}]");
        }

        lines.Add("");
        lines.Add("## User Question");
        lines.Add(question);

        return string.Join("\n", lines);
    }
}
