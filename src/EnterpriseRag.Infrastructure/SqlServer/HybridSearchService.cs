using System.Globalization;
using System.Text;
using EnterpriseRag.Core.Configuration;
using EnterpriseRag.Core.Interfaces;
using EnterpriseRag.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseRag.Infrastructure.SqlServer;

public class HybridSearchService : ISearchService
{
    private readonly DatabaseSettings _dbSettings;
    private readonly SearchSettings _searchSettings;
    private readonly ILogger<HybridSearchService> _logger;

    public HybridSearchService(
        IOptions<DatabaseSettings> dbSettings,
        IOptions<SearchSettings> searchSettings,
        ILogger<HybridSearchService> logger)
    {
        _dbSettings = dbSettings.Value;
        _searchSettings = searchSettings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SearchResult>> HybridSearchAsync(
        SearchRequest request, CancellationToken ct = default)
    {
        var topK = request.TopK > 0 ? request.TopK : _searchSettings.DefaultTopK;

        _logger.LogDebug("Executing hybrid search: TopK={TopK}, Category={Category}, Color={Color}",
            topK, request.Category ?? "(any)", request.Color ?? "(any)");

        var results = new List<SearchResult>();

        await using var connection = new SqlConnection(_dbSettings.ConnectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand("dbo.usp_HybridProductSearch", connection)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@QueryVector", VectorToString(request.QueryVector));
        command.Parameters.AddWithValue("@TopK", topK);
        command.Parameters.AddWithValue("@Category",
            (object?)request.Category ?? DBNull.Value);
        command.Parameters.AddWithValue("@Color",
            (object?)request.Color ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            results.Add(new SearchResult
            {
                ProductKey = reader.GetInt32(reader.GetOrdinal("ProductKey")),
                EnglishProductName = reader.GetString(reader.GetOrdinal("EnglishProductName")),
                EnglishDescription = reader.IsDBNull(reader.GetOrdinal("EnglishDescription"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("EnglishDescription")),
                ProductSubcategoryName = reader.IsDBNull(reader.GetOrdinal("ProductSubcategoryName"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("ProductSubcategoryName")),
                Color = reader.IsDBNull(reader.GetOrdinal("Color"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Color")),
                SimilarityScore = reader.GetDouble(reader.GetOrdinal("SimilarityScore"))
            });
        }

        _logger.LogDebug("Hybrid search returned {Count} results", results.Count);
        return results;
    }

    /// <summary>
    /// Converts a float array to the SQL Server VECTOR literal format: [0.123,0.456,...]
    /// </summary>
    private static string VectorToString(float[] vector)
    {
        var sb = new StringBuilder(vector.Length * 10);
        sb.Append('[');
        for (int i = 0; i < vector.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(vector[i].ToString("G", CultureInfo.InvariantCulture));
        }
        sb.Append(']');
        return sb.ToString();
    }
}
