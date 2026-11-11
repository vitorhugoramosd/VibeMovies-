using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace VibeMovies.Infrastructure.Persistence;

/// <summary>
/// Evolução de schema idempotente para bancos criados com <c>EnsureCreated</c> (que nunca altera tabelas existentes).
/// Roda logo após o EnsureCreated e adiciona as colunas novas que faltarem (SQLite: PRAGMA table_info;
/// PostgreSQL: information_schema.columns) com ALTER TABLE ... ADD COLUMN.
/// </summary>
public static class DatabaseSchemaUpgrader
{
    public sealed record ColumnSpec(string Table, string Column, string SqliteDefinition, string PostgresDefinition);

    /// <summary>Colunas adicionadas depois da primeira versão. Acrescente aqui a cada coluna nova.</summary>
    public static readonly IReadOnlyList<ColumnSpec> Columns = new List<ColumnSpec>
    {
        new("Users", "MyProvidersCsv", "TEXT NOT NULL DEFAULT ''", "character varying(500) NOT NULL DEFAULT ''")
    };

    /// <summary>Retorna a lista de colunas adicionadas ("Tabela.Coluna").</summary>
    public static async Task<List<string>> UpgradeAsync(VibeMoviesDbContext db, ILogger? logger = null, CancellationToken ct = default)
    {
        var added = new List<string>();
        if (!db.Database.IsRelational()) return added;

        var provider = db.Database.ProviderName ?? string.Empty;
        var isSqlite = provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase);
        var isPostgres = provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
        if (!isSqlite && !isPostgres)
        {
            logger?.LogWarning("Schema upgrader: provider {Provider} não suportado; nenhuma coluna verificada.", provider);
            return added;
        }

        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await db.Database.OpenConnectionAsync(ct);

        try
        {
            foreach (var table in Columns.GroupBy(c => c.Table))
            {
                var existing = isSqlite
                    ? await GetSqliteColumnsAsync(connection, table.Key, ct)
                    : await GetPostgresColumnsAsync(connection, table.Key, ct);

                if (existing.Count == 0) continue; // tabela ainda não existe (EnsureCreated cuidará dela)

                foreach (var col in table)
                {
                    if (existing.Contains(col.Column)) continue;

                    var definition = isSqlite ? col.SqliteDefinition : col.PostgresDefinition;
                    var sql = $"ALTER TABLE \"{col.Table}\" ADD COLUMN \"{col.Column}\" {definition}";
                    await using var cmd = connection.CreateCommand();
                    cmd.CommandText = sql;
                    await cmd.ExecuteNonQueryAsync(ct);

                    added.Add($"{col.Table}.{col.Column}");
                    logger?.LogInformation("Schema upgrader: coluna {Table}.{Column} adicionada.", col.Table, col.Column);
                }
            }
        }
        finally
        {
            if (openedHere) await db.Database.CloseConnectionAsync();
        }

        return added;
    }

    private static async Task<HashSet<string>> GetSqliteColumnsAsync(DbConnection connection, string table, CancellationToken ct)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info(\"{table.Replace("\"", "\"\"")}\")";
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var nameOrdinal = reader.GetOrdinal("name");
        while (await reader.ReadAsync(ct)) columns.Add(reader.GetString(nameOrdinal));
        return columns;
    }

    private static async Task<HashSet<string>> GetPostgresColumnsAsync(DbConnection connection, string table, CancellationToken ct)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT column_name FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = @table";
        var p = cmd.CreateParameter();
        p.ParameterName = "@table";
        p.Value = table;
        cmd.Parameters.Add(p);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) columns.Add(reader.GetString(0));
        return columns;
    }
}
