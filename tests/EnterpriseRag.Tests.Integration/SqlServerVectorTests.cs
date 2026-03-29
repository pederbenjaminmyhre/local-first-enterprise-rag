using FluentAssertions;
using Microsoft.Data.SqlClient;

namespace EnterpriseRag.Tests.Integration;

/// <summary>
/// Integration tests that require a running SQL Server 2025 instance
/// with the AdventureWorksDW2020 database and schema migrations applied.
/// </summary>
[Trait("Category", "Integration")]
public class SqlServerVectorTests
{
    private const string ConnectionString =
        "Server=localhost,1433;Database=AdventureWorksDW2020;User Id=sa;Password=YourStrong!Pass2025;TrustServerCertificate=true;";

    private static bool IsSqlServerRunning()
    {
        try
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [SkippableFact]
    public async Task CanConnect_ToSqlServer2025()
    {
        Skip.IfNot(IsSqlServerRunning(), "SQL Server is not running on localhost:1433");

        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand("SELECT @@VERSION", conn);
        var version = (string?)await cmd.ExecuteScalarAsync();

        version.Should().NotBeNull();
        version.Should().Contain("Microsoft SQL Server");
    }

    [SkippableFact]
    public async Task DimProduct_HasDescriptionVectorColumn()
    {
        Skip.IfNot(IsSqlServerRunning(), "SQL Server is not running on localhost:1433");

        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        const string sql = """
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = 'DimProduct' AND COLUMN_NAME = 'DescriptionVector'
            """;

        await using var cmd = new SqlCommand(sql, conn);
        var count = (int)(await cmd.ExecuteScalarAsync())!;

        count.Should().Be(1, "DescriptionVector column should exist after migration 002");
    }

    [SkippableFact]
    public async Task HybridSearchSproc_Exists()
    {
        Skip.IfNot(IsSqlServerRunning(), "SQL Server is not running on localhost:1433");

        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        const string sql = """
            SELECT COUNT(*) FROM sys.procedures
            WHERE name = 'usp_HybridProductSearch'
            """;

        await using var cmd = new SqlCommand(sql, conn);
        var count = (int)(await cmd.ExecuteScalarAsync())!;

        count.Should().Be(1, "usp_HybridProductSearch should exist after migration 003");
    }

    [SkippableFact]
    public async Task DimProduct_HasRows()
    {
        Skip.IfNot(IsSqlServerRunning(), "SQL Server is not running on localhost:1433");

        await using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.DimProduct", conn);
        var count = (int)(await cmd.ExecuteScalarAsync())!;

        count.Should().BeGreaterThan(0, "AdventureWorksDW2020 DimProduct should have product rows");
    }
}
