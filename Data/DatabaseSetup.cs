using Dapper;
using Microsoft.Data.SqlClient;
namespace ProofTrail.Data;

public static class DatabaseSetup
{
    public static async Task InitializeAsync(SqlDatabase database)
    {
        var settings = new SqlConnectionStringBuilder(database.ConnectionString);
        var name = settings.InitialCatalog;
        if (string.IsNullOrWhiteSpace(name) || new[] { "master", "model", "msdb", "tempdb" }.Contains(name, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Specify a separate application database, such as ProofTrailDb.");
        settings.InitialCatalog = "master";
        await using (var master = new SqlConnection(settings.ConnectionString))
        {
            await master.OpenAsync();
            // The identifier cannot be a SQL parameter; escape it with SQL Server bracket quoting.
            if (await master.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sys.databases WHERE name = @name", new { name }) == 0)
                await master.ExecuteAsync($"CREATE DATABASE [{name.Replace("]", "]]")}]", commandTimeout: 60);
        }
        using var stream = typeof(DatabaseSetup).Assembly.GetManifestResourceStream("ProofTrail.Data.Schema.sql")
            ?? throw new InvalidOperationException("Schema.sql embedded resource missing.");
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync();
        await database.BeginAsync();
        try
        {
            await (await database.OpenAsync()).ExecuteAsync(sql, transaction: database.Transaction);
            await database.CommitAsync();
        }
        catch { await database.RollbackAsync(); throw; }
    }
}
