namespace EnterpriseRag.Core.Interfaces;

public interface IEmbeddingClient
{
    Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default);
}
