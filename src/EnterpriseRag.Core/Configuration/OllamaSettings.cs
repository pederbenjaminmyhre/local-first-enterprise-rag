namespace EnterpriseRag.Core.Configuration;

public class OllamaSettings
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string EmbeddingModel { get; set; } = "mxbai-embed-large";
    public string GenerationModel { get; set; } = "llama3.2";
}
