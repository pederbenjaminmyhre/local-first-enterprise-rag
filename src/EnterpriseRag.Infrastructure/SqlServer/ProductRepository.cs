using EnterpriseRag.Core.Configuration;
using EnterpriseRag.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace EnterpriseRag.Infrastructure.SqlServer;

public class ProductRepository : IProductRepository
{
    private readonly DatabaseSettings _dbSettings;

    public ProductRepository(IOptions<DatabaseSettings> dbSettings)
    {
        _dbSettings = dbSettings.Value;
    }

    public async Task<IReadOnlyList<string>> GetDistinctCategoriesAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT DISTINCT sc.EnglishProductSubcategoryName
            FROM dbo.DimProduct p
            INNER JOIN dbo.DimProductSubcategory sc
                ON p.ProductSubcategoryKey = sc.ProductSubcategoryKey
            WHERE p.DescriptionVector IS NOT NULL
            ORDER BY sc.EnglishProductSubcategoryName
            """;

        return await QueryStringListAsync(sql, ct);
    }

    public async Task<IReadOnlyList<string>> GetDistinctColorsAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT DISTINCT Color
            FROM dbo.DimProduct
            WHERE DescriptionVector IS NOT NULL
              AND Color IS NOT NULL
            ORDER BY Color
            """;

        return await QueryStringListAsync(sql, ct);
    }

    private async Task<IReadOnlyList<string>> QueryStringListAsync(string sql, CancellationToken ct)
    {
        var results = new List<string>();

        await using var connection = new SqlConnection(_dbSettings.ConnectionString);
        await connection.OpenAsync(ct);

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            results.Add(reader.GetString(0));
        }

        return results;
    }
}
