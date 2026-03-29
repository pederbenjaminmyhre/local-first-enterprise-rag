using EnterpriseRag.Core.Models;

namespace EnterpriseRag.Core.Interfaces;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResult>> HybridSearchAsync(SearchRequest request, CancellationToken ct = default);
}
