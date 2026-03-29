namespace EnterpriseRag.Core.Models;

public class SearchResult
{
    public int ProductKey { get; set; }
    public string EnglishProductName { get; set; } = string.Empty;
    public string? EnglishDescription { get; set; }
    public string? ProductSubcategoryName { get; set; }
    public string? Color { get; set; }
    public double SimilarityScore { get; set; }
}
