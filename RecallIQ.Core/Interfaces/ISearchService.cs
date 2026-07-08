using RecallIQ.Core.Models;

namespace RecallIQ.Core.Interfaces;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int maxResults = 20, double minScore = 0.25, CancellationToken cancellationToken = default);
}
