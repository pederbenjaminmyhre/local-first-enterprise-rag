using EnterpriseRag.Core.Configuration;
using EnterpriseRag.Core.Interfaces;
using EnterpriseRag.Infrastructure.Ollama;
using EnterpriseRag.Infrastructure.Rag;
using EnterpriseRag.Infrastructure.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseRag.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Bind configuration sections
        services.Configure<OllamaSettings>(configuration.GetSection(OllamaSettings.SectionName));
        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));
        services.Configure<SearchSettings>(configuration.GetSection(SearchSettings.SectionName));

        // Register Ollama HttpClient with base URL from config
        var ollamaUrl = configuration.GetSection(OllamaSettings.SectionName)
            .GetValue<string>("BaseUrl") ?? "http://localhost:11434";

        services.AddHttpClient<IEmbeddingClient, OllamaEmbeddingClient>(client =>
        {
            client.BaseAddress = new Uri(ollamaUrl);
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        services.AddHttpClient<IGenerationClient, OllamaGenerationClient>(client =>
        {
            client.BaseAddress = new Uri(ollamaUrl);
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        // Register SQL Server services
        services.AddScoped<ISearchService, HybridSearchService>();

        // Register RAG orchestrator
        services.AddScoped<IRagOrchestrator, RagOrchestrator>();

        return services;
    }
}
