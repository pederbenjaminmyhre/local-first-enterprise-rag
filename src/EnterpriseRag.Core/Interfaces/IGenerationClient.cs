namespace EnterpriseRag.Core.Interfaces;

public interface IGenerationClient
{
    Task<string> GenerateAsync(string prompt, CancellationToken ct = default);
    IAsyncEnumerable<string> GenerateStreamAsync(string prompt, CancellationToken ct = default);
}
