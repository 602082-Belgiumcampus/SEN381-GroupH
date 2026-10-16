using System.Net;
using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.RequestQueries;
using Microsoft.Extensions.DependencyInjection;

namespace CivicConnect.Integration.Tests;

[Collection(DatabaseCollection.Name)]
public class AuthorisationTests(CivicConnectAppFactory app) : IntegrationTest(app)
{
    [DatabaseFact]
    public async Task TC_AZ_01_Requester_cannot_open_another_requesters_request()
    {
        var requesterA = await CreateUserAsync(Role.Requester);
        var requesterB = await CreateUserAsync(Role.Requester);
        var request = await SubmitRequestAsync(requesterA, "Private request of requester A");

        var client = await SignInAsync(requesterB);
        var response = await client.GetAsync($"/Requests/Details/{request.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("Private request of requester A", await response.Content.ReadAsStringAsync());
    }

    [DatabaseFact]
    public async Task TC_AZ_02_Requester_posting_Accept_directly_is_refused_and_nothing_changes()
    {
        var requester = await CreateUserAsync(Role.Requester);
        var request = await SubmitRequestAsync(requester);
        var client = await SignInAsync(requester);

        // The page offers no Accept button to a requester, so post the form by hand
        var detailsUrl = $"/Requests/Details/{request.Id}";
        var response = await PostFormAsync(client, detailsUrl, $"{detailsUrl}?handler=Accept", new() { ["Version"] = "1" });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var saved = await LoadRequestAsync(request.Id);
        Assert.Equal(RequestStatus.Submitted, saved.Status);
        Assert.Equal(1, saved.Version);
        Assert.Equal(1, await CountAuditEntriesAsync(request.Id));
    }

    [DatabaseFact]
    public async Task TC_AZ_03_Staff_without_close_permission_cannot_close_a_resolved_request()
    {
        var requester = await CreateUserAsync(Role.Requester);
        var supervisor = await CreateUserAsync(Role.Supervisor);
        var staff = await CreateUserAsync(Role.ServiceStaff);
        var request = await SubmitRequestAsync(requester);

        await AssignAsync(request.Id, supervisor, staff);
        Assert.True((await ApplyAsync(request.Id, new LifecycleCommand(LifecycleEvent.Start), staff.Actor)).Succeeded);
        Assert.True((await ApplyAsync(request.Id, new LifecycleCommand(LifecycleEvent.Resolve, Note: "Bulb replaced"), staff.Actor)).Succeeded);
        var resolved = await LoadRequestAsync(request.Id);

        var result = await ApplyAsync(request.Id, new LifecycleCommand(LifecycleEvent.Close), staff.Actor);

        Assert.Equal(LifecycleServiceOutcome.Forbidden, result.Outcome);
        var saved = await LoadRequestAsync(request.Id);
        Assert.Equal(RequestStatus.Resolved, saved.Status);
        Assert.Equal(resolved.Version, saved.Version);
        Assert.Null(saved.ClosedAt);
    }

    [DatabaseFact]
    public async Task TC_AZ_04_Anonymous_visitor_is_sent_to_sign_in_for_protected_pages()
    {
        var client = CreateClient();

        foreach (var url in new[] { "/Requests", "/Requests/New", "/Staff", $"/Requests/Details/{Guid.NewGuid()}" })
        {
            var response = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith("/Account/Login", response.Headers.Location!.PathAndQuery);
        }
    }

    [DatabaseFact]
    public async Task TC_AZ_05_Work_queue_follows_role_and_assignment()
    {
        var requester = await CreateUserAsync(Role.Requester);
        var supervisor = await CreateUserAsync(Role.Supervisor);
        var staffA = await CreateUserAsync(Role.ServiceStaff);
        var staffB = await CreateUserAsync(Role.ServiceStaff);

        var assignedToA = await SubmitRequestAsync(requester, "Assigned to staff A");
        await AssignAsync(assignedToA.Id, supervisor, staffA);
        var unassigned = await SubmitRequestAsync(requester, "Still unassigned");

        var queueA = await QueueIdsAsync(staffA);
        var queueB = await QueueIdsAsync(staffB);
        var queueSupervisor = await QueueIdsAsync(supervisor);

        Assert.Contains(assignedToA.Id, queueA);
        Assert.Contains(unassigned.Id, queueA);
        Assert.DoesNotContain(assignedToA.Id, queueB);
        Assert.Contains(unassigned.Id, queueB);
        Assert.Contains(assignedToA.Id, queueSupervisor);
        Assert.Contains(unassigned.Id, queueSupervisor);
    }

    [DatabaseFact]
    public async Task Requester_is_refused_the_staff_work_queue()
    {
        var client = await SignInAsync(await CreateUserAsync(Role.Requester));

        var response = await client.GetAsync("/Staff");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", response.Headers.Location!.PathAndQuery);
    }

    [DatabaseFact]
    public async Task Deactivated_user_cannot_sign_in()
    {
        var user = await CreateUserAsync(Role.Requester, isActive: false);
        var client = CreateClient();

        var response = await PostFormAsync(client, "/Account/Login", "/Account/Login", new()
        {
            ["Input.Email"] = user.Email,
            ["Input.Password"] = user.Password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // the form again, with an error
        Assert.Contains("The email address or password is incorrect.", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Requests")).StatusCode);
    }

    // Regression: the sign-in fallback policy used to block CSS and JS, so every page was unstyled
    [DatabaseFact]
    public async Task Stylesheets_and_scripts_load_without_signing_in()
    {
        var client = CreateClient();

        foreach (var url in new[] { "/css/site.css", "/lib/bootstrap/dist/css/bootstrap.min.css", "/js/site.js" })
        {
            var response = await client.GetAsync(url);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{url} returned {(int)response.StatusCode}");
        }
    }

    // Regression: staff used to land on a 404 after rejecting, because they can no longer see the request
    [DatabaseFact]
    public async Task Staff_who_reject_a_request_are_sent_back_to_the_work_queue()
    {
        var requester = await CreateUserAsync(Role.Requester);
        var staff = await CreateUserAsync(Role.ServiceStaff);
        var request = await SubmitRequestAsync(requester);
        var client = await SignInAsync(staff);

        var detailsUrl = $"/Requests/Details/{request.Id}";
        var response = await PostFormAsync(client, detailsUrl, $"{detailsUrl}?handler=Reject", new()
        {
            ["Version"] = "1",
            ["RejectionReason"] = "Not something we handle"
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Staff", response.Headers.Location!.OriginalString);
        Assert.Equal(RequestStatus.Rejected, (await LoadRequestAsync(request.Id)).Status);
    }

    private Task<List<Guid>> QueueIdsAsync(TestUser user) =>
        WithServicesAsync(async services =>
        {
            var queue = await services.GetRequiredService<IRequestQueries>().GetStaffQueueAsync(user.Actor, 1, 100);
            return queue.Items.Select(r => r.Id).ToList();
        });
}
