using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace CivicConnect.Integration.Tests;

/// <summary>
/// Starts the real web app once per test run against a new, empty PostgreSQL schema.
/// Start-up applies the migrations and seeds roles and categories into that schema;
/// the schema is dropped again when the run ends.
/// </summary>
public sealed class CivicConnectAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string schema = $"it_{Guid.NewGuid():N}";
    private string? connectionString;

    public async Task InitializeAsync()
    {
        if (!TestDatabase.IsConfigured)
            return;

        await ExecuteAsync($"CREATE SCHEMA \"{schema}\"");
        connectionString = new NpgsqlConnectionStringBuilder(TestDatabase.ConnectionString) { SearchPath = schema }.ConnectionString;

        // Build the host now, so migrations and seeding run before the first test
        _ = Server;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:CivicConnect", connectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        if (connectionString is null)
            return;

        NpgsqlConnection.ClearAllPools();
        await ExecuteAsync($"DROP SCHEMA \"{schema}\" CASCADE");
    }

    private static async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(TestDatabase.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<CivicConnectAppFactory>
{
    public const string Name = "Database";
}
