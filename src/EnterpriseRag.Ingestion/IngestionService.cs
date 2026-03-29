using System.Diagnostics;
using System.Globalization;
using System.Text;
using EnterpriseRag.Core.Configuration;
using EnterpriseRag.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnterpriseRag.Ingestion;

public class IngestionService : BackgroundService
{
    private readonly IEmbeddingClient _embeddingClient;
    private readonly DatabaseSettings _dbSettings;
    private readonly ILogger<IngestionService> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    private const int BatchSize = 50;

    public IngestionService(
        IEmbeddingClient embeddingClient,
        IOptions<DatabaseSettings> dbSettings,
        ILogger<IngestionService> logger,
        IHostApplicationLifetime lifetime)
    {
        _embeddingClient = embeddingClient;
        _dbSettings = dbSettings.Value;
        _logger = logger;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunIngestionAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingestion pipeline failed");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }

    private async Task RunIngestionAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting ingestion pipeline...");

        // Load all products that need embedding
        var products = await LoadProductsWithoutVectorsAsync(ct);

        if (products.Count == 0)
        {
            _logger.LogInformation("All products already have embeddings. Nothing to do.");
            return;
        }

        _logger.LogInformation("Found {Count} products to embed", products.Count);

        int completed = 0;
        int failed = 0;

        // Process in batches
        foreach (var batch in products.Chunk(BatchSize))
        {
            var updates = new List<(int ProductKey, float[] Vector)>();

            foreach (var (productKey, description) in batch)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var vector = await _embeddingClient.GetEmbeddingAsync(description, ct);
                    updates.Add((productKey, vector));
                    completed++;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogWarning(ex, "Failed to embed ProductKey={ProductKey}, skipping", productKey);
                }
            }

            if (updates.Count > 0)
            {
                await BulkUpdateVectorsAsync(updates, ct);
            }

            _logger.LogInformation("Progress: {Completed}/{Total} embedded ({Failed} failed)",
                completed, products.Count, failed);
        }

        stopwatch.Stop();
        _logger.LogInformation(
            "Ingestion complete: {Completed}/{Total} products embedded in {Elapsed}",
            completed, products.Count, stopwatch.Elapsed.ToString(@"m\m\ ss\s"));
    }

    private async Task<List<(int ProductKey, string Description)>> LoadProductsWithoutVectorsAsync(
        CancellationToken ct)
    {
        var products = new List<(int, string)>();

        await using var connection = new SqlConnection(_dbSettings.ConnectionString);
        await connection.OpenAsync(ct);

        const string sql = """
            SELECT ProductKey, EnglishDescription
            FROM dbo.DimProduct
            WHERE DescriptionVector IS NULL
              AND EnglishDescription IS NOT NULL
              AND LEN(EnglishDescription) > 0
            ORDER BY ProductKey
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            products.Add((
                reader.GetInt32(0),
                reader.GetString(1)
            ));
        }

        return products;
    }

    private async Task BulkUpdateVectorsAsync(
        List<(int ProductKey, float[] Vector)> updates, CancellationToken ct)
    {
        await using var connection = new SqlConnection(_dbSettings.ConnectionString);
        await connection.OpenAsync(ct);

        foreach (var (productKey, vector) in updates)
        {
            var vectorStr = VectorToString(vector);

            const string sql = """
                UPDATE dbo.DimProduct
                SET DescriptionVector = CAST(@Vector AS VECTOR(1024))
                WHERE ProductKey = @ProductKey
                """;

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@Vector", vectorStr);
            command.Parameters.AddWithValue("@ProductKey", productKey);

            await command.ExecuteNonQueryAsync(ct);
        }
    }

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
