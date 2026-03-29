using EnterpriseRag.Core.Models;

namespace EnterpriseRag.Core.Interfaces;

public interface IRagOrchestrator
{
    Task<RagResponse> AskAsync(string question, string? category = null, string? color = null, CancellationToken ct = default);
}
