namespace EnterpriseRag.Core.Configuration;

public class DatabaseSettings
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = string.Empty;
    public int TopK { get; set; } = 5;
}
