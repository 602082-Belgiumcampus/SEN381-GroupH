// Load test for ASR-05 (NFR-005, NFR-006) under the AS-02 workload: 50 concurrent users and a
// year's worth of requests (10 000). Targets: 95% of list and view operations under 2 s and 95% of
// submissions under 3 s.
//
// Run against a scratch database, never the real one: --seed inserts requests directly.
//   dotnet run --project tests/CivicConnect.LoadTests -c Release -- --base-url http://localhost:5160 --seed 10000
// Credentials come from environment variables so they are not on the command line:
//   LOADTEST_DB         connection string of the database the app under test uses (for --seed)
//   LOADTEST_PASSWORD   password of the staff and requester accounts used by the virtual users

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;

var options = Options.Parse(args);
var password = Environment.GetEnvironmentVariable("LOADTEST_PASSWORD")
    ?? throw new InvalidOperationException("Set LOADTEST_PASSWORD to the password of the test accounts.");

if (options.Seed > 0)
{
    var connectionString = Environment.GetEnvironmentVariable("LOADTEST_DB")
        ?? throw new InvalidOperationException("Set LOADTEST_DB to seed requests.");
    await Seeder.SeedAsync(connectionString, options.RequesterEmail, options.Seed);
}

var results = new ConcurrentBag<Sample>();
var stopAt = DateTimeOffset.UtcNow.AddSeconds(options.DurationSeconds);
Console.WriteLine($"Running {options.Users} virtual users for {options.DurationSeconds} s against {options.BaseUrl} ...");

var clock = Stopwatch.StartNew();
var users = Enumerable.Range(0, options.Users).Select(i =>
    i % 2 == 0
        ? VirtualUser.RunStaffAsync(options, password, stopAt, results, seed: i)
        : VirtualUser.RunRequesterAsync(options, password, stopAt, results, seed: i));
await Task.WhenAll(users);
clock.Stop();

var report = Report.Build(options, results.ToList(), clock.Elapsed);
Console.WriteLine(report);
if (options.OutFile is not null)
{
    await File.WriteAllTextAsync(options.OutFile, report);
    Console.WriteLine($"Written to {options.OutFile}");
}

sealed record Sample(string Operation, bool IsSubmit, double Milliseconds, bool Ok);

sealed record Options(
    string BaseUrl, int Users, int DurationSeconds, int Seed,
    string StaffEmail, string RequesterEmail, string? OutFile)
{
    public static Options Parse(string[] args)
    {
        string Get(string name, string fallback)
        {
            var at = Array.IndexOf(args, name);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : fallback;
        }

        return new Options(
            Get("--base-url", "http://localhost:5160").TrimEnd('/'),
            int.Parse(Get("--users", "50")),
            int.Parse(Get("--duration", "120")),
            int.Parse(Get("--seed", "0")),
            Get("--staff-email", "supervisor@civicconnect.local"),
            Get("--requester-email", "requester@civicconnect.local"),
            Get("--out", "") is { Length: > 0 } outFile ? outFile : null);
    }
}

static partial class VirtualUser
{
    // Think time between actions, as a real person reads the page before clicking on
    private static Task ThinkAsync(Random random) => Task.Delay(random.Next(1000, 3000));

    public static async Task RunStaffAsync(Options o, string password, DateTimeOffset stopAt, ConcurrentBag<Sample> results, int seed)
    {
        var random = new Random(seed);
        using var client = NewClient(o);
        await SignInAsync(client, o.StaffEmail, password, results);

        string[] searches = ["CC-2026-0001", "light", "drain", "repair"];
        while (DateTimeOffset.UtcNow < stopAt)
        {
            var queue = await TimedGetAsync(client, "Work queue", "/Staff", results);
            await ThinkAsync(random);

            await TimedGetAsync(client, "Search and filter", $"/Staff?Search={searches[random.Next(searches.Length)]}&Status=Accepted&Sort=Newest", results);
            await ThinkAsync(random);

            if (FirstRequestLink(queue) is string details)
            {
                await TimedGetAsync(client, "Request detail", details, results);
                await ThinkAsync(random);
            }

            await TimedGetAsync(client, "Reports", "/Reports", results);
            await ThinkAsync(random);
        }
    }

    public static async Task RunRequesterAsync(Options o, string password, DateTimeOffset stopAt, ConcurrentBag<Sample> results, int seed)
    {
        var random = new Random(seed);
        using var client = NewClient(o);
        await SignInAsync(client, o.RequesterEmail, password, results);

        while (DateTimeOffset.UtcNow < stopAt)
        {
            var mine = await TimedGetAsync(client, "My requests", "/Requests", results);
            await ThinkAsync(random);

            var form = await TimedGetAsync(client, "Submit form", "/Requests/New", results);
            await ThinkAsync(random);

            var categoryId = CategoryOption().Match(form) is { Success: true } m ? m.Groups[1].Value : null;
            if (categoryId is not null && Token(form) is string token)
            {
                await TimedPostAsync(client, "Submit request", "/Requests/New", new()
                {
                    ["Input.CategoryId"] = categoryId,
                    ["Input.Title"] = $"Load test request {Guid.NewGuid():N}"[..40],
                    ["Input.Description"] = "Created by the CivicConnect load test.",
                    ["__RequestVerificationToken"] = token
                }, results);
                await ThinkAsync(random);
            }

            if (FirstRequestLink(mine) is string details)
            {
                await TimedGetAsync(client, "Request detail", details, results);
                await ThinkAsync(random);
            }
        }
    }

    private static HttpClient NewClient(Options o) =>
        new(new HttpClientHandler { CookieContainer = new CookieContainer(), AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(o.BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

    private static async Task SignInAsync(HttpClient client, string email, string password, ConcurrentBag<Sample> results)
    {
        var page = await TimedGetAsync(client, "Sign-in page", "/Account/Login", results);
        var response = await TimedPostAsync(client, "Sign in", "/Account/Login", new()
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["__RequestVerificationToken"] = Token(page) ?? string.Empty
        }, results);

        if (response != HttpStatusCode.Redirect)
            throw new InvalidOperationException($"Sign-in as {email} failed ({(int)response}). Check the account and LOADTEST_PASSWORD.");
    }

    private static async Task<string> TimedGetAsync(HttpClient client, string operation, string url, ConcurrentBag<Sample> results)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            using var response = await client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();
            results.Add(new Sample(operation, false, watch.Elapsed.TotalMilliseconds, response.StatusCode == HttpStatusCode.OK));
            return body;
        }
        catch (Exception)
        {
            results.Add(new Sample(operation, false, watch.Elapsed.TotalMilliseconds, false));
            return string.Empty;
        }
    }

    private static async Task<HttpStatusCode> TimedPostAsync(
        HttpClient client, string operation, string url, Dictionary<string, string> form, ConcurrentBag<Sample> results)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            using var response = await client.PostAsync(url, new FormUrlEncodedContent(form));
            await response.Content.ReadAsStringAsync();
            // A successful form post redirects (post-redirect-get)
            results.Add(new Sample(operation, operation == "Submit request", watch.Elapsed.TotalMilliseconds, response.StatusCode == HttpStatusCode.Redirect));
            return response.StatusCode;
        }
        catch (Exception)
        {
            results.Add(new Sample(operation, operation == "Submit request", watch.Elapsed.TotalMilliseconds, false));
            return 0;
        }
    }

    private static string? Token(string html) =>
        AntiforgeryToken().Match(html) is { Success: true } m ? WebUtility.HtmlDecode(m.Groups[1].Value) : null;

    private static string? FirstRequestLink(string html) =>
        RequestLink().Match(html) is { Success: true } m ? m.Groups[1].Value : null;

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryToken();

    [GeneratedRegex("href=\"(/Requests/Details/[0-9a-f-]{36})\"")]
    private static partial Regex RequestLink();

    [GeneratedRegex("<option value=\"([0-9a-f-]{36})\"")]
    private static partial Regex CategoryOption();
}

static class Seeder
{
    /// <summary>
    /// Inserts <paramref name="count"/> requests spread over the past year, with a realistic mix of
    /// statuses, plus one audit entry each, in a single SQL statement per table.
    /// </summary>
    public static async Task SeedAsync(string connectionString, string requesterEmail, int count)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        const string sql = """
            WITH requester AS (
                SELECT "Id" FROM "AspNetUsers" WHERE "NormalizedEmail" = upper(@email)
            ),
            categories AS (
                SELECT array_agg("Id") AS ids FROM "Categories" WHERE "IsActive"
            ),
            rows AS (
                SELECT i,
                       (ARRAY['Submitted','Accepted','Rejected','Resolved','Closed'])[1 + i % 5] AS status,
                       now() - make_interval(mins => (random() * 525600)::int) AS created
                FROM generate_series(1, @count) AS i
            ),
            inserted AS (
                INSERT INTO "ServiceRequests"
                    ("Id", "Reference", "RequesterId", "CategoryId", "ContactChannel", "Title", "Description",
                     "Status", "Version", "RejectionReason", "ResolutionNote", "CreatedAt", "UpdatedAt", "ClosedAt")
                SELECT gen_random_uuid(),
                       'CC-' || extract(year FROM now())::int || '-' || lpad(nextval('"ServiceRequestReferenceNumbers"')::text, 6, '0'),
                       (SELECT "Id" FROM requester),
                       categories.ids[1 + (rows.i % array_length(categories.ids, 1))],
                       'Web',
                       (ARRAY['Street light out','Blocked drain','Broken bench','Graffiti to remove','Pothole repair'])[1 + rows.i % 5] || ' #' || rows.i,
                       'Seeded by the load test.',
                       rows.status,
                       1,
                       CASE WHEN rows.status = 'Rejected' THEN 'Seeded rejection' END,
                       CASE WHEN rows.status IN ('Resolved', 'Closed') THEN 'Seeded resolution' END,
                       rows.created, rows.created,
                       CASE WHEN rows.status = 'Closed' THEN rows.created + interval '3 days' END
                FROM rows, categories
                RETURNING "Id", "RequesterId", "Status", "CreatedAt"
            )
            INSERT INTO "AuditEntries" ("RequestId", "ActorId", "EventType", "FromStatus", "ToStatus", "Detail", "OccurredAt")
            SELECT "Id", "RequesterId", 'Submit', NULL, "Status", 'seeded', "CreatedAt" FROM inserted;
            """;

        await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 300 };
        command.Parameters.AddWithValue("email", requesterEmail);
        command.Parameters.AddWithValue("count", count);

        var watch = Stopwatch.StartNew();
        var rows = await command.ExecuteNonQueryAsync();
        Console.WriteLine($"Seeded {count} requests ({rows} audit rows) in {watch.Elapsed.TotalSeconds:0.0} s.");
    }
}

static class Report
{
    public static string Build(Options o, List<Sample> samples, TimeSpan elapsed)
    {
        static double Percentile(List<double> sorted, double p) =>
            sorted.Count == 0 ? 0 : sorted[Math.Clamp((int)Math.Ceiling(p / 100 * sorted.Count) - 1, 0, sorted.Count - 1)];

        var text = new StringBuilder();
        text.AppendLine($"Load test, {DateTimeOffset.Now:yyyy-MM-dd HH:mm zzz}");
        text.AppendLine($"Target: {o.BaseUrl}; {o.Users} virtual users; {elapsed.TotalSeconds:0} s; think time 1-3 s");
        text.AppendLine($"Requests: {samples.Count}; throughput {samples.Count / elapsed.TotalSeconds:0.0}/s; errors {samples.Count(s => !s.Ok)}");
        text.AppendLine();
        text.AppendLine("| Operation | Count | Errors | p50 (ms) | p95 (ms) | p99 (ms) | Max (ms) |");
        text.AppendLine("|-|-:|-:|-:|-:|-:|-:|");

        foreach (var group in samples.GroupBy(s => s.Operation).OrderBy(g => g.Key))
        {
            var sorted = group.Select(s => s.Milliseconds).Order().ToList();
            text.AppendLine($"| {group.Key} | {group.Count()} | {group.Count(s => !s.Ok)} | {Percentile(sorted, 50):0} | {Percentile(sorted, 95):0} | {Percentile(sorted, 99):0} | {sorted.Last():0} |");
        }

        // ASR-05: list and view operations (every GET except sign-in) vs submissions
        var views = samples.Where(s => !s.IsSubmit && s.Operation is not ("Sign in" or "Sign-in page")).Select(s => s.Milliseconds).Order().ToList();
        var submits = samples.Where(s => s.IsSubmit).Select(s => s.Milliseconds).Order().ToList();
        var viewP95 = Percentile(views, 95);
        var submitP95 = Percentile(submits, 95);

        text.AppendLine();
        text.AppendLine("| ASR-05 check | p95 (ms) | Target (ms) | Result |");
        text.AppendLine("|-|-:|-:|-|");
        text.AppendLine($"| List and view operations ({views.Count}) | {viewP95:0} | 2000 | {(viewP95 < 2000 ? "Pass" : "Fail")} |");
        text.AppendLine($"| Submissions ({submits.Count}) | {submitP95:0} | 3000 | {(submits.Count > 0 && submitP95 < 3000 ? "Pass" : "Fail")} |");
        return text.ToString();
    }
}
