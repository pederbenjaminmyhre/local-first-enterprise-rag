namespace EnterpriseRag.Core.Configuration;

public class SearchSettings
{
    public const string SectionName = "Search";

    public int DefaultTopK { get; set; } = 5;
    public double SimilarityThreshold { get; set; } = 0.3;
}
