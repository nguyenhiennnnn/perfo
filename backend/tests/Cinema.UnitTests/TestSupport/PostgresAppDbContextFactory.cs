using Cinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cinema.UnitTests.TestSupport;

public sealed class PostgresAppDbContextHandle : IDisposable
{
    private readonly string _masterConnectionString;
    private readonly string _dbName;

    public AppDbContext Db { get; }

    public PostgresAppDbContextHandle(string masterConnectionString)
    {
        _masterConnectionString = masterConnectionString;
        _dbName = "ut_" + Guid.NewGuid().ToString("N");

        using var adminConn = new NpgsqlConnection(masterConnectionString);
        adminConn.Open();
        using (var cmd = adminConn.CreateCommand())
        {
            cmd.CommandText = $"CREATE DATABASE \"{_dbName}\"";
            cmd.ExecuteNonQuery();
        }

        var connStr = BuildConnectionString(masterConnectionString, _dbName);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connStr)
            .Options;

        Db = new AppDbContext(options);

        using (var setupConn = new NpgsqlConnection(connStr))
        {
            setupConn.Open();
            using var extCmd = setupConn.CreateCommand();
            extCmd.CommandText = "CREATE EXTENSION IF NOT EXISTS unaccent;";
            extCmd.ExecuteNonQuery();
        }

        Db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Db.Dispose();
        NpgsqlConnection.ClearAllPools();

        var noPoolCs = new NpgsqlConnectionStringBuilder(_masterConnectionString)
        {
            Pooling = false,
        }.ConnectionString;

        using var adminConn = new NpgsqlConnection(noPoolCs);
        adminConn.Open();

        using (var killCmd = adminConn.CreateCommand())
        {
            killCmd.CommandText = $"""
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{_dbName}' AND pid <> pg_backend_pid()
                """;
            killCmd.ExecuteNonQuery();
        }

        using (var dropCmd = adminConn.CreateCommand())
        {
            dropCmd.CommandText = $"DROP DATABASE IF EXISTS \"{_dbName}\"";
            dropCmd.ExecuteNonQuery();
        }
    }

    private static string BuildConnectionString(string master, string dbName)
    {
        var builder = new NpgsqlConnectionStringBuilder(master) { Database = dbName };
        return builder.ConnectionString;
    }
}
