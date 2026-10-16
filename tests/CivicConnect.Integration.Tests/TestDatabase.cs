using Microsoft.Extensions.Configuration;

namespace CivicConnect.Integration.Tests;

/// <summary>
/// Where the integration tests find PostgreSQL. CI sets the CIVICCONNECT_TEST_DB environment variable;
/// on a developer machine the tests reuse the web app's user-secrets connection string, so no extra
/// setup is needed. Every run works in its own throwaway schema, so local data is never touched.
/// </summary>
public static class TestDatabase
{
    public const string EnvironmentVariable = "CIVICCONNECT_TEST_DB";

    // The web project's UserSecretsId (src/CivicConnect.Web/CivicConnect.Web.csproj)
    private const string WebUserSecretsId = "civicconnect-web-sen381-grouph";

    public static string? ConnectionString { get; } = Find();

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);

    private static string? Find()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
            return fromEnvironment;

        return new ConfigurationBuilder()
            .AddUserSecrets(WebUserSecretsId)
            .Build()
            .GetConnectionString("CivicConnect");
    }
}

//A test that needs PostgreSQL. It is skipped, not failed, when no database is configured
public sealed class DatabaseFactAttribute : FactAttribute
{
    public DatabaseFactAttribute()
    {
        // In CI a missing database is a broken pipeline, so the tests fail there instead of skipping
        var runningInCi = string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);
        if (!TestDatabase.IsConfigured && !runningInCi)
            Skip = $"No test database: set {TestDatabase.EnvironmentVariable} or the web app's ConnectionStrings:CivicConnect user-secret.";
    }
}
