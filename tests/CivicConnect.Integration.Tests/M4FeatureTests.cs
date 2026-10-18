using System.Net;
using CivicConnect.Infrastructure.Identity;
using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Modules.Administration;
using CivicConnect.Modules.Feedback;
using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.Reporting;
using CivicConnect.Modules.RequestIntake;
using CivicConnect.Modules.RequestQueries;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CivicConnect.Integration.Tests;

/// <summary>
/// M4 features against PostgreSQL and the running app: FR-006, FR-008, FR-012, FR-014 to FR-017,
/// FR-020 (TC-IN-04), FR-021 and FR-022 (TC-AZ-06, AC-038, AC-039) and reopening (OD-05).
/// </summary>
[Collection(DatabaseCollection.Name)]
public class M4FeatureTests(CivicConnectAppFactory app) : IntegrationTest(app)
{
    // --- FR-012 record action --------------------------------------------------------------------

    [DatabaseFact]
    public async Task Assignee_records_an_action_which_is_logged_and_audited_without_changing_status()
    {
        var (request, _, staff) = await AssignedRequestAsync();

        var result = await WithServicesAsync(s => s.GetRequiredService<ILifecycleService>()
            .RecordActionAsync(request.Id, "  Ordered a replacement bulb  ", staff.Actor));

        Assert.Equal(LifecycleServiceOutcome.Applied, result.Outcome);
        var action = await WithDbAsync(db => db.RequestActions.AsNoTracking().SingleAsync(a => a.RequestId == request.Id));
        Assert.Equal("Ordered a replacement bulb", action.Text);
        Assert.Equal(staff.Id, action.AuthorId);
        Assert.True(await WithDbAsync(db => db.AuditEntries.AnyAsync(a => a.RequestId == request.Id && a.EventType == "RecordAction")));
        Assert.Equal(RequestStatus.Assigned, (await LoadRequestAsync(request.Id)).Status);
    }

    [DatabaseFact]
    public async Task Other_staff_member_cannot_record_an_action()
    {
        var (request, _, _) = await AssignedRequestAsync();
        var otherStaff = await CreateUserAsync(Role.ServiceStaff);

        var result = await WithServicesAsync(s => s.GetRequiredService<ILifecycleService>()
            .RecordActionAsync(request.Id, "Not my request", otherStaff.Actor));

        // Staff B cannot even see a request assigned to staff A, so it looks like it does not exist
        Assert.Equal(LifecycleServiceOutcome.NotFound, result.Outcome);
        Assert.False(await WithDbAsync(db => db.RequestActions.AnyAsync(a => a.RequestId == request.Id)));
    }

    // --- OD-05 reopen ----------------------------------------------------------------------------

    [DatabaseFact]
    public async Task Supervisor_reopens_a_resolved_request_and_the_requester_is_told()
    {
        var (request, supervisor, staff) = await AssignedRequestAsync();
        Assert.True((await ApplyAsync(request.Id, new LifecycleCommand(LifecycleEvent.Start), staff.Actor)).Succeeded);
        Assert.True((await ApplyAsync(request.Id, new LifecycleCommand(LifecycleEvent.Resolve, Note: "Bulb replaced"), staff.Actor)).Succeeded);

        var staffTry = await ApplyAsync(request.Id, new LifecycleCommand(LifecycleEvent.Reopen, Reason: "Still dark"), staff.Actor);
        var result = await ApplyAsync(request.Id, new LifecycleCommand(LifecycleEvent.Reopen, Reason: "Still dark"), supervisor.Actor);

        Assert.Equal(LifecycleServiceOutcome.Forbidden, staffTry.Outcome);
        Assert.Equal(LifecycleServiceOutcome.Applied, result.Outcome);
        var saved = await LoadRequestAsync(request.Id);
        Assert.Equal(RequestStatus.InProgress, saved.Status);
        Assert.Equal(staff.Id, saved.AssignedToId);
        Assert.Null(saved.ResolutionNote);
        Assert.True(await WithDbAsync(db => db.FeedbackNotices.AnyAsync(n => n.RequestId == request.Id && n.Message.Contains("reopened"))));
    }

    // --- FR-006 feedback notices -----------------------------------------------------------------

    [DatabaseFact]
    public async Task Requester_gets_a_notice_for_each_agreed_event_and_can_mark_them_read()
    {
        var requester = await CreateUserAsync(Role.Requester);
        var staff = await CreateUserAsync(Role.ServiceStaff);
        var request = await SubmitRequestAsync(requester);

        Assert.True((await ApplyAsync(request.Id, new LifecycleCommand(LifecycleEvent.Reject, Reason: "Duplicate of CC-1"), staff.Actor)).Succeeded);

        var notices = await WithServicesAsync(s => s.GetRequiredService<IFeedbackNotices>().GetForRecipientAsync(requester.Id, 1, 10));
        var notice = Assert.Single(notices.Items);
        Assert.Contains(request.Reference, notice.Message);
        Assert.Contains("Duplicate of CC-1", notice.Message);
        Assert.False(notice.IsRead);

        // The unread count shows in the menu; marking read clears it
        var client = await SignInAsync(requester);
        var noticesPage = await client.GetStringAsync("/Notices");
        var badgeAt = noticesPage.IndexOf("cc-count-badge", StringComparison.Ordinal);
        Assert.True(badgeAt >= 0, "No unread badge in the menu");
        var badge = noticesPage.Substring(badgeAt, Math.Min(120, noticesPage.Length - badgeAt));
        Assert.Contains("\">1<span", badge);
        Assert.Contains(" unread</span>", badge);
        var marked = await PostFormAsync(client, "/Notices", "/Notices?handler=MarkAllRead", new());
        Assert.Equal(HttpStatusCode.Redirect, marked.StatusCode);
        Assert.Equal(0, await WithServicesAsync(s => s.GetRequiredService<IFeedbackNotices>().CountUnreadAsync(requester.Id)));
    }

    [DatabaseFact]
    public async Task A_user_never_sees_someone_elses_notices()
    {
        var requesterA = await CreateUserAsync(Role.Requester);
        var requesterB = await CreateUserAsync(Role.Requester);
        var staff = await CreateUserAsync(Role.ServiceStaff);
        var request = await SubmitRequestAsync(requesterA, "Only A should hear about this");
        await ApplyAsync(request.Id, new LifecycleCommand(LifecycleEvent.Accept), staff.Actor);

        var client = await SignInAsync(requesterB);
        var page = await client.GetStringAsync("/Notices");

        Assert.DoesNotContain(request.Reference, page);
    }

    // --- FR-008 search, filter, sort -------------------------------------------------------------

    [DatabaseFact]
    public async Task Queue_search_matches_reference_or_title_and_filters_combine()
    {
        var requester = await CreateUserAsync(Role.Requester);
        var supervisor = await CreateUserAsync(Role.Supervisor);
        var marker = Guid.NewGuid().ToString("N")[..8];
        var first = await SubmitRequestAsync(requester, $"Leaking tap {marker}");
        var second = await SubmitRequestAsync(requester, $"Broken window {marker}");
        await ApplyAsync(second.Id, new LifecycleCommand(LifecycleEvent.Accept), supervisor.Actor);

        var byTitle = await QueueAsync(supervisor, new RequestFilter(Search: marker));
        var byReference = await QueueAsync(supervisor, new RequestFilter(Search: first.Reference.ToLowerInvariant()));
        var byStatus = await QueueAsync(supervisor, new RequestFilter(Search: marker, Status: RequestStatus.Accepted));
        var newestFirst = await QueueAsync(supervisor, new RequestFilter(Search: marker, Sort: RequestSort.Newest));

        Assert.Equal(new[] { first.Id, second.Id }, byTitle);
        Assert.Equal(new[] { first.Id }, byReference);
        Assert.Equal(new[] { second.Id }, byStatus);
        Assert.Equal(new[] { second.Id, first.Id }, newestFirst);
    }

    [DatabaseFact]
    public async Task Search_wildcards_are_matched_literally_and_never_widen_visibility()
    {
        var requesterA = await CreateUserAsync(Role.Requester);
        var requesterB = await CreateUserAsync(Role.Requester);
        await SubmitRequestAsync(requesterA, "A request requester B must not find");

        var everything = await WithServicesAsync(s => s.GetRequiredService<IRequestQueries>()
            .GetMyRequestsAsync(requesterB.Id, new RequestFilter(Search: "%"), 1, 100));

        Assert.Equal(0, everything.TotalCount);
    }

    [DatabaseFact]
    public async Task Search_and_filters_work_from_the_query_string()
    {
        var requester = await CreateUserAsync(Role.Requester);
        var marker = Guid.NewGuid().ToString("N")[..8];
        var request = await SubmitRequestAsync(requester, $"Graffiti on the wall {marker}");
        var client = await SignInAsync(requester);

        var hit = await client.GetStringAsync($"/Requests?Search={marker}&Status=Submitted&Sort=Oldest");
        var miss = await client.GetStringAsync($"/Requests?Search={marker}&Status=Closed");
        var junk = await client.GetAsync("/Requests?Status=NotAStatus&Sort=Sideways&CategoryId=nope");

        Assert.Contains(request.Reference, hit);
        Assert.DoesNotContain(request.Reference, miss);
        Assert.Equal(HttpStatusCode.OK, junk.StatusCode);
    }

    // --- FR-014 to FR-017 reports and overdue ----------------------------------------------------

    [DatabaseFact]
    public async Task Overdue_report_lists_open_requests_past_their_category_target()
    {
        var requester = await CreateUserAsync(Role.Requester);
        var categoryId = await NewCategoryAsync(targetDays: 3);
        var late = await SubmitRequestAsync(requester, categoryId, "Late one");
        var onTime = await SubmitRequestAsync(requester, categoryId, "On time one");
        await BackdateAsync(late.Id, days: 5);
        await BackdateAsync(onTime.Id, days: 2);

        var filter = new ReportFilter(CategoryId: categoryId);
        var overdue = await WithServicesAsync(s => s.GetRequiredService<IReportQueries>().GetOverdueAsync(filter, 1, 50));
        var summary = await WithServicesAsync(s => s.GetRequiredService<IReportQueries>().GetSummaryAsync(filter));

        var item = Assert.Single(overdue.Items);
        Assert.Equal(late.Id, item.Id);
        Assert.Equal(2, item.DaysOverdue);
        Assert.Equal((2, 2, 1), (summary.Total, summary.Open, summary.Overdue));
    }

    [DatabaseFact]
    public async Task Changing_a_category_target_changes_what_is_overdue()
    {
        var requester = await CreateUserAsync(Role.Requester);
        var categoryId = await NewCategoryAsync(targetDays: 10);
        var request = await SubmitRequestAsync(requester, categoryId, "Borderline");
        await BackdateAsync(request.Id, days: 6);
        var filter = new ReportFilter(CategoryId: categoryId);

        var before = await WithServicesAsync(s => s.GetRequiredService<IReportQueries>().GetSummaryAsync(filter));
        await WithServicesAsync(s => s.GetRequiredService<IAdministration>().UpdateCategoryAsync(categoryId, new CategoryInput($"Cat {categoryId:N}", 5)));
        var after = await WithServicesAsync(s => s.GetRequiredService<IReportQueries>().GetSummaryAsync(filter));

        Assert.Equal(0, before.Overdue);
        Assert.Equal(1, after.Overdue);
    }

    [DatabaseFact]
    public async Task Staff_activity_report_counts_actions_from_the_audit_trail()
    {
        var (request, supervisor, staff) = await AssignedRequestAsync();
        await ApplyAsync(request.Id, new LifecycleCommand(LifecycleEvent.Start), staff.Actor);
        await WithServicesAsync(s => s.GetRequiredService<ILifecycleService>().RecordActionAsync(request.Id, "Checked the wiring", staff.Actor));

        var activity = await WithServicesAsync(s => s.GetRequiredService<IReportQueries>().GetStaffActivityAsync(new ReportFilter()));

        // Test users all share a display name per role, so find the rows by what they did
        Assert.Contains(activity, r => r.StaffName == "Test ServiceStaff" && r.Started >= 1 && r.ActionsRecorded >= 1);
        Assert.Contains(activity, r => r.StaffName == "Test Supervisor" && r.Accepted >= 1 && r.Assigned >= 1);
        Assert.DoesNotContain(activity, r => r.StaffName == "Test Requester");
    }

    [DatabaseFact]
    public async Task Reports_are_for_supervisors_and_management_only_and_show_no_requester_names()
    {
        var requester = await CreateUserAsync(Role.Requester);
        await SubmitRequestAsync(requester);

        foreach (var role in new[] { Role.Requester, Role.ServiceStaff, Role.Administrator })
        {
            var denied = await (await SignInAsync(await CreateUserAsync(role))).GetAsync("/Reports");
            Assert.StartsWith("/Account/AccessDenied", denied.Headers.Location!.PathAndQuery);
        }

        var page = await (await SignInAsync(await CreateUserAsync(Role.Management))).GetStringAsync("/Reports");
        Assert.Contains("Overdue requests", page);
        Assert.DoesNotContain("Test Requester", page);
    }

    // --- FR-020 log on behalf (TC-IN-04) ---------------------------------------------------------

    [DatabaseFact]
    public async Task TC_IN_04_Requester_cannot_log_on_behalf_and_nothing_is_created()
    {
        var requester = await CreateUserAsync(Role.Requester);
        var email = $"walkin-{Guid.NewGuid():N}@test.local";

        var service = await WithServicesAsync(async s => await s.GetRequiredService<ISubmitRequest>()
            .SubmitOnBehalfAsync(requester.Actor, await OnBehalfAsync(email, "Walk In")));
        var page = await (await SignInAsync(requester)).GetAsync("/Staff/LogRequest");

        Assert.True(service.IsForbidden);
        Assert.StartsWith("/Account/AccessDenied", page.Headers.Location!.PathAndQuery);
        Assert.Null(await FindUserAsync(email));
    }

    [DatabaseFact]
    public async Task Staff_log_a_request_for_a_new_person_who_can_later_register_and_see_it()
    {
        var staff = await CreateUserAsync(Role.ServiceStaff);
        var email = $"phoned-{Guid.NewGuid():N}@test.local";

        var result = await WithServicesAsync(async s => await s.GetRequiredService<ISubmitRequest>()
            .SubmitOnBehalfAsync(staff.Actor, await OnBehalfAsync(email, "Thandi Phone")));

        Assert.True(result.Succeeded);
        var saved = await LoadRequestAsync(result.RequestId!.Value);
        var person = await FindUserAsync(email);
        Assert.NotNull(person);
        Assert.Null(person!.PasswordHash);
        Assert.Equal(person.Id, saved.RequesterId);
        Assert.Equal(staff.Id, saved.LoggedById);
        Assert.Equal(ContactChannels.Phone, saved.ContactChannel);
        Assert.True(await WithDbAsync(db => db.AuditEntries.AnyAsync(a => a.RequestId == saved.Id && a.EventType == "LoggedOnBehalf" && a.ActorId == staff.Id)));

        // The person registers with the same email, which sets their password and shows the request
        var client = CreateClient();
        var password = $"Aa1-{Guid.NewGuid():N}";
        var registered = await PostFormAsync(client, "/Account/Register", "/Account/Register", new()
        {
            ["Input.DisplayName"] = "Thandi",
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.ConfirmPassword"] = password
        });
        Assert.Equal(HttpStatusCode.Redirect, registered.StatusCode);
        Assert.Contains(result.Reference!, await client.GetStringAsync("/Requests"));
    }

    [DatabaseFact]
    public async Task Staff_log_a_request_for_an_existing_requester_without_creating_another_account()
    {
        var staff = await CreateUserAsync(Role.Supervisor);
        var requester = await CreateUserAsync(Role.Requester);

        var result = await WithServicesAsync(async s => await s.GetRequiredService<ISubmitRequest>()
            .SubmitOnBehalfAsync(staff.Actor, await OnBehalfAsync(requester.Email.ToUpperInvariant(), name: null)));

        Assert.True(result.Succeeded);
        Assert.Equal(requester.Id, (await LoadRequestAsync(result.RequestId!.Value)).RequesterId);
    }

    [DatabaseFact]
    public async Task Staff_cannot_log_a_request_against_a_staff_account()
    {
        var staff = await CreateUserAsync(Role.ServiceStaff);
        var otherStaff = await CreateUserAsync(Role.ServiceStaff);

        var result = await WithServicesAsync(async s => await s.GetRequiredService<ISubmitRequest>()
            .SubmitOnBehalfAsync(staff.Actor, await OnBehalfAsync(otherStaff.Email, name: null)));

        Assert.Equal(nameof(OnBehalfInput.RequesterEmail), Assert.Single(result.Errors).Field);
    }

    // --- FR-021 / FR-022 administration (TC-AZ-06, AC-038, AC-039) -------------------------------

    [DatabaseFact]
    public async Task TC_AZ_06_Non_administrators_are_refused_every_admin_page_and_change_nothing()
    {
        var target = await CreateUserAsync(Role.ServiceStaff);
        var categoryId = await NewCategoryAsync(targetDays: 7);

        foreach (var role in new[] { Role.Requester, Role.ServiceStaff, Role.Supervisor, Role.Management })
        {
            var client = await SignInAsync(await CreateUserAsync(role));
            foreach (var url in new[] { "/Admin/Users", "/Admin/Users/New", $"/Admin/Users/Edit/{target.Id}", "/Admin/Categories", $"/Admin/Categories/Edit/{categoryId}" })
            {
                var response = await client.GetAsync(url);
                Assert.True(response.Headers.Location?.PathAndQuery.StartsWith("/Account/AccessDenied") == true, $"{role} reached {url}");
            }

            // A hand-made POST is refused before the handler runs (no anti-forgery token is needed to prove it)
            var post = await client.PostAsync($"/Admin/Users/Edit/{target.Id}?handler=Deactivate", new FormUrlEncodedContent([]));
            Assert.NotEqual(HttpStatusCode.OK, post.StatusCode);
        }

        Assert.True((await FindUserAsync(target.Email))!.IsActive);
    }

    [DatabaseFact]
    public async Task AC_038_Deactivated_user_is_signed_out_and_cannot_sign_in_again()
    {
        var admin = await CreateUserAsync(Role.Administrator);
        var staff = await CreateUserAsync(Role.ServiceStaff);
        var staffClient = await SignInAsync(staff);
        Assert.Equal(HttpStatusCode.OK, (await staffClient.GetAsync("/Staff")).StatusCode);

        var adminClient = await SignInAsync(admin);
        var url = $"/Admin/Users/Edit/{staff.Id}";
        var response = await PostFormAsync(adminClient, url, $"{url}?handler=Deactivate", new());

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False((await FindUserAsync(staff.Email))!.IsActive);
        var signIn = await PostFormAsync(CreateClient(), "/Account/Login", "/Account/Login", new()
        {
            ["Input.Email"] = staff.Email,
            ["Input.Password"] = staff.Password
        });
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode); // the form again, not a redirect into the site
    }

    [DatabaseFact]
    public async Task AC_038_Role_change_takes_effect()
    {
        var admin = await CreateUserAsync(Role.Administrator);
        var user = await CreateUserAsync(Role.Requester);

        var result = await WithServicesAsync(s => s.GetRequiredService<IAdministration>()
            .SetRolesAsync(admin.Id, user.Id, [Role.ServiceStaff]));

        Assert.True(result.Succeeded);
        var client = await SignInAsync(user);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Staff")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Requests/New")).StatusCode);
    }

    [DatabaseFact]
    public async Task Administrator_cannot_deactivate_themselves()
    {
        var admin = await CreateUserAsync(Role.Administrator);

        var result = await WithServicesAsync(s => s.GetRequiredService<IAdministration>().SetActiveAsync(admin.Id, admin.Id, false));

        Assert.False(result.Succeeded);
        Assert.True((await FindUserAsync(admin.Email))!.IsActive);
    }

    [DatabaseFact]
    public async Task Administrator_creates_a_staff_account_that_can_sign_in()
    {
        var email = $"newstaff-{Guid.NewGuid():N}@test.local";
        var password = $"Aa1-{Guid.NewGuid():N}";

        var result = await WithServicesAsync(s => s.GetRequiredService<IAdministration>()
            .CreateUserAsync(new NewUserInput("New Staff", email, Role.ServiceStaff, password)));

        Assert.True(result.Succeeded);
        var client = await SignInAsync(new TestUser(result.Id!.Value, email, password, Role.ServiceStaff));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Staff")).StatusCode);
    }

    [DatabaseFact]
    public async Task AC_039_Deactivated_category_disappears_from_intake_but_existing_requests_keep_it()
    {
        var requester = await CreateUserAsync(Role.Requester);
        var categoryId = await NewCategoryAsync(targetDays: 4);
        var existing = await SubmitRequestAsync(requester, categoryId, "Filed before the change");
        var name = await WithDbAsync(db => db.Categories.Where(c => c.Id == categoryId).Select(c => c.Name).SingleAsync());

        var result = await WithServicesAsync(s => s.GetRequiredService<IAdministration>()
            .UpdateCategoryAsync(categoryId, new CategoryInput(name, 4, IsActive: false)));

        Assert.True(result.Succeeded);
        var active = await WithServicesAsync(s => s.GetRequiredService<ISubmitRequest>().GetActiveCategoriesAsync());
        Assert.DoesNotContain(active, c => c.Id == categoryId);
        Assert.Equal(categoryId, (await LoadRequestAsync(existing.Id)).CategoryId);
    }

    [DatabaseFact]
    public async Task Category_names_must_be_unique()
    {
        var categoryId = await NewCategoryAsync(targetDays: 4);
        var name = await WithDbAsync(db => db.Categories.Where(c => c.Id == categoryId).Select(c => c.Name).SingleAsync());

        var result = await WithServicesAsync(s => s.GetRequiredService<IAdministration>()
            .CreateCategoryAsync(new CategoryInput(name.ToUpperInvariant(), 4)));

        Assert.Equal(nameof(CategoryInput.Name), Assert.Single(result.Errors).Field);
    }

    [DatabaseFact]
    public async Task Health_endpoint_reports_healthy_without_signing_in()
    {
        var response = await CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    // --- helpers -----------------------------------------------------------------------------------

    private async Task<(SubmittedRequest Request, TestUser Supervisor, TestUser Staff)> AssignedRequestAsync()
    {
        var requester = await CreateUserAsync(Role.Requester);
        var supervisor = await CreateUserAsync(Role.Supervisor);
        var staff = await CreateUserAsync(Role.ServiceStaff);
        var request = await SubmitRequestAsync(requester);
        await AssignAsync(request.Id, supervisor, staff);
        return (request, supervisor, staff);
    }

    private Task<List<Guid>> QueueAsync(TestUser user, RequestFilter filter) =>
        WithServicesAsync(async s =>
            (await s.GetRequiredService<IRequestQueries>().GetStaffQueueAsync(user.Actor, filter, 1, 100)).Items.Select(r => r.Id).ToList());

    private Task<Guid> NewCategoryAsync(int targetDays) =>
        WithDbAsync(async db =>
        {
            var category = new Category { Id = Guid.CreateVersion7(), Name = $"Cat {Guid.NewGuid():N}", IsActive = true, TargetResolutionDays = targetDays };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return category.Id;
        });

    private async Task<SubmittedRequest> SubmitRequestAsync(TestUser requester, Guid categoryId, string title)
    {
        var result = await WithServicesAsync(s => s.GetRequiredService<ISubmitRequest>()
            .SubmitAsync(requester.Id, new SubmitRequestInput(categoryId, title, "Details", null)));
        Assert.True(result.Succeeded);
        return new SubmittedRequest(result.RequestId!.Value, result.Reference!);
    }

    /// <summary>
    /// Moves a request's creation time into the past, as if it had been waiting that long.
    /// One hour short of whole days, so the test's own running time cannot tip a count over a day boundary.
    /// </summary>
    private Task BackdateAsync(Guid requestId, int days) =>
        WithDbAsync(async db =>
        {
            var request = await db.ServiceRequests.SingleAsync(r => r.Id == requestId);
            request.CreatedAt -= TimeSpan.FromDays(days) - TimeSpan.FromHours(1);
            return await db.SaveChangesAsync();
        });

    private async Task<OnBehalfInput> OnBehalfAsync(string email, string? name) =>
        new(email, name, ContactChannels.Phone, new SubmitRequestInput(await ActiveCategoryIdAsync(), "Pothole outside the library", "Reported by phone", null));

    private Task<AppUser?> FindUserAsync(string email) =>
        WithServicesAsync(s => s.GetRequiredService<UserManager<AppUser>>().FindByEmailAsync(email));
}
