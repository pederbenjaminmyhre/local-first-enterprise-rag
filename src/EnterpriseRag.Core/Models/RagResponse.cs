namespace EnterpriseRag.Core.Models;

public class RagResponse
{
    public required string Answer { get; init; }
    public required IReadOnlyList<SearchResult> Sources { get; init; }
    public TimeSpan ElapsedTime { get; init; }
}
