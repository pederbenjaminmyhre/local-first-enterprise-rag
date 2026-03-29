namespace EnterpriseRag.Core.Models;

public class SearchRequest
{
    public required float[] QueryVector { get; init; }
    public string? Category { get; init; }
    public string? Color { get; init; }
    public int TopK { get; init; } = 5;
}
