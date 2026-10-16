using System.Net;
using System.Text.RegularExpressions;
using CivicConnect.Infrastructure.Identity;
using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.RequestIntake;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CivicConnect.Integration.Tests;

public sealed record TestUser(Guid Id, string Email, string Password, Role Role)
{
    public Actor Actor => ActorFactory.FromRoleNames(Id, [Role.ToString()]);
}

public sealed record SubmittedRequest(Guid Id, string Reference);

/// <summary>
/// Shared helpers. Every test creates its own users and requests, so tests do not depend on each
/// other or on what earlier tests left in the database.
/// </summary>
[Collection(DatabaseCollection.Name)]
public abstract partial class IntegrationTest(CivicConnectAppFactory app)
{
    protected CivicConnectAppFactory App { get; } = app;

    protected async Task<T> WithServicesAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = App.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    protected Task<T> WithDbAsync<T>(Func<CivicConnectDbContext, Task<T>> action) =>
        WithServicesAsync(services => action(services.GetRequiredService<CivicConnectDbContext>()));

    protected Task<TestUser> CreateUserAsync(Role role, bool isActive = true) =>
        WithServicesAsync(async services =>
        {
            var users = services.GetRequiredService<UserManager<AppUser>>();
            var email = $"{role.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@test.local";
            // Generated per run, so no password is written in the repository
            var password = $"Aa1-{Guid.NewGuid():N}";

            var user = new AppUser
            {
                Id = Guid.CreateVersion7(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = $"Test {role}",
                IsActive = isActive,
                CreatedAt = DateTimeOffset.UtcNow
            };

            AssertSucceeded(await users.CreateAsync(user, password));
            AssertSucceeded(await users.AddToRoleAsync(user, role.ToString()));
            return new TestUser(user.Id, email, password, role);
        });

    protected Task<Guid> ActiveCategoryIdAsync() =>
        WithServicesAsync(async services =>
            (await services.GetRequiredService<ISubmitRequest>().GetActiveCategoriesAsync()).First().Id);

    protected async Task<SubmittedRequest> SubmitRequestAsync(TestUser requester, string title = "Broken light in the hall")
    {
        var categoryId = await ActiveCategoryIdAsync();
        var result = await WithServicesAsync(services => services.GetRequiredService<ISubmitRequest>()
            .SubmitAsync(requester.Id, new SubmitRequestInput(categoryId, title, "The light has been off for a week.", null)));

        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Message)));
        return new SubmittedRequest(result.RequestId!.Value, result.Reference!);
    }

    /// <summary>Applies a lifecycle command the way a fresh page would: with the request's current version.</summary>
    protected async Task<LifecycleServiceResult> ApplyAsync(Guid requestId, LifecycleCommand command, Actor actor)
    {
        var version = (await LoadRequestAsync(requestId)).Version;
        return await ApplyAsync(requestId, version, command, actor);
    }

    protected Task<LifecycleServiceResult> ApplyAsync(Guid requestId, int expectedVersion, LifecycleCommand command, Actor actor) =>
        WithServicesAsync(services => services.GetRequiredService<ILifecycleService>()
            .ApplyAsync(requestId, expectedVersion, command, actor));

    protected Task<ServiceRequest> LoadRequestAsync(Guid requestId) =>
        WithDbAsync(db => db.ServiceRequests.AsNoTracking().SingleAsync(r => r.Id == requestId));

    protected Task<int> CountAuditEntriesAsync(Guid requestId) =>
        WithDbAsync(db => db.AuditEntries.CountAsync(a => a.RequestId == requestId));

    /// <summary>Takes a request from Submitted to Assigned (assigned to <paramref name="assignee"/>).</summary>
    protected async Task AssignAsync(Guid requestId, TestUser supervisor, TestUser assignee)
    {
        Assert.True((await ApplyAsync(requestId, new LifecycleCommand(LifecycleEvent.Accept), supervisor.Actor)).Succeeded);
        Assert.True((await ApplyAsync(requestId, new LifecycleCommand(LifecycleEvent.Assign, assignee.Id), supervisor.Actor)).Succeeded);
    }

    // helpers 

    protected HttpClient CreateClient() =>
        App.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    protected async Task<HttpClient> SignInAsync(TestUser user)
    {
        var client = CreateClient();
        var response = await PostFormAsync(client, "/Account/Login", "/Account/Login", new()
        {
            ["Input.Email"] = user.Email,
            ["Input.Password"] = user.Password
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    // Loads <paramref name="formPage"/> for its anti-forgery token, then posts the form to <paramref name="postUrl"/>
    protected static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string formPage, string postUrl, Dictionary<string, string> fields)
    {
        var page = await client.GetStringAsync(formPage);
        var token = AntiforgeryToken().Match(page);
        Assert.True(token.Success, $"No anti-forgery token found on {formPage}");

        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(token.Groups[1].Value);
        return await client.PostAsync(postUrl, new FormUrlEncodedContent(fields));
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryToken();

    private static void AssertSucceeded(IdentityResult result) =>
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
}
