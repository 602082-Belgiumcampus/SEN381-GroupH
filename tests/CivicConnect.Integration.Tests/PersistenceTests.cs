using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;
using CivicConnect.Modules.RequestIntake;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CivicConnect.Integration.Tests;

[Collection(DatabaseCollection.Name)]
public class PersistenceTests(CivicConnectAppFactory app) : IntegrationTest(app)
{
    [DatabaseFact]
    public async Task TC_PE_01_Submitted_request_is_committed_and_found_by_its_reference()
    {
        var requester = await CreateUserAsync(Role.Requester);

        var submitted = await SubmitRequestAsync(requester);

        // Read back in a new scope, so this is what the database holds, not what EF still tracks
        var saved = await WithDbAsync(db => db.ServiceRequests.AsNoTracking().SingleAsync(r => r.Reference == submitted.Reference));
        Assert.Equal(submitted.Id, saved.Id);
        Assert.Equal(requester.Id, saved.RequesterId);
        Assert.Equal(RequestStatus.Submitted, saved.Status);
        Assert.Equal(1, saved.Version);

        var audit = await WithDbAsync(db => db.AuditEntries.AsNoTracking().SingleAsync(a => a.RequestId == submitted.Id));
        Assert.Equal("Submit", audit.EventType);
        Assert.Null(audit.FromStatus);
        Assert.Equal(RequestStatus.Submitted, audit.ToStatus);
    }

    [DatabaseFact]
    public async Task TC_IN_01_Two_submissions_get_two_different_references()
    {
        var requester = await CreateUserAsync(Role.Requester);

        var first = await SubmitRequestAsync(requester, "First request");
        var second = await SubmitRequestAsync(requester, "Second request");

        Assert.NotEqual(first.Reference, second.Reference);
        Assert.Matches(@"^CC-\d{4}-\d{6}$", first.Reference);
    }

    [DatabaseFact]
    public async Task TC_IN_02_Invalid_submission_is_rejected_and_nothing_is_saved()
    {
        var requester = await CreateUserAsync(Role.Requester);

        var result = await WithServicesAsync(services => services.GetRequiredService<ISubmitRequest>()
            .SubmitAsync(requester.Id, new SubmitRequestInput(null, " ", null, null)));

        Assert.False(result.Succeeded);
        Assert.Equal(
            new[] { nameof(SubmitRequestInput.CategoryId), nameof(SubmitRequestInput.Title), nameof(SubmitRequestInput.Description) },
            result.Errors.Select(e => e.Field));
        Assert.Equal(0, await WithDbAsync(db => db.ServiceRequests.CountAsync(r => r.RequesterId == requester.Id)));
    }

    [DatabaseFact]
    public async Task TC_IN_03_Inactive_category_is_rejected_and_nothing_is_saved()
    {
        var requester = await CreateUserAsync(Role.Requester);
        var inactiveId = await WithDbAsync(async db =>
        {
            var category = new Category { Id = Guid.CreateVersion7(), Name = $"Retired {Guid.NewGuid():N}", IsActive = false, TargetResolutionDays = 5 };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return category.Id;
        });

        var result = await WithServicesAsync(services => services.GetRequiredService<ISubmitRequest>()
            .SubmitAsync(requester.Id, new SubmitRequestInput(inactiveId, "Title", "Description", null)));

        Assert.Equal(nameof(SubmitRequestInput.CategoryId), Assert.Single(result.Errors).Field);
        Assert.Equal(0, await WithDbAsync(db => db.ServiceRequests.CountAsync(r => r.RequesterId == requester.Id)));
    }

    [DatabaseFact]
    public async Task TC_PE_02_Second_change_from_a_stale_page_is_a_conflict_and_adds_no_audit_entry()
    {
        var requester = await CreateUserAsync(Role.Requester);
        var staffA = await CreateUserAsync(Role.ServiceStaff);
        var staffB = await CreateUserAsync(Role.ServiceStaff);
        var request = await SubmitRequestAsync(requester);

        // Both staff members opened the request while it was at version 1
        var first = await ApplyAsync(request.Id, 1, new LifecycleCommand(LifecycleEvent.Accept), staffA.Actor);
        var second = await ApplyAsync(request.Id, 1, new LifecycleCommand(LifecycleEvent.Reject, Reason: "Duplicate"), staffB.Actor);

        Assert.Equal(LifecycleServiceOutcome.Applied, first.Outcome);
        Assert.Equal(2, first.NewVersion);
        Assert.Equal(LifecycleServiceOutcome.Conflict, second.Outcome);

        var saved = await LoadRequestAsync(request.Id);
        Assert.Equal(RequestStatus.Accepted, saved.Status);
        Assert.Equal(2, saved.Version);
        Assert.Equal(2, await CountAuditEntriesAsync(request.Id)); // Submit + Accept only
    }

    [DatabaseFact]
    public async Task TC_PE_02_Database_refuses_a_save_based_on_an_old_version()
    {
        var requester = await CreateUserAsync(Role.Requester);
        var request = await SubmitRequestAsync(requester);

        // Two contexts read the same row, then both try to save a change to it
        await using var scopeA = App.Services.CreateAsyncScope();
        await using var scopeB = App.Services.CreateAsyncScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<CivicConnectDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<CivicConnectDbContext>();
        var rowA = await dbA.ServiceRequests.SingleAsync(r => r.Id == request.Id);
        var rowB = await dbB.ServiceRequests.SingleAsync(r => r.Id == request.Id);

        rowA.Status = RequestStatus.Accepted;
        rowA.Version++;
        await dbA.SaveChangesAsync();

        rowB.Status = RequestStatus.Rejected;
        rowB.RejectionReason = "Too late";
        rowB.Version++;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());

        Assert.Equal(RequestStatus.Accepted, (await LoadRequestAsync(request.Id)).Status);
    }

    [DatabaseFact]
    public async Task TC_PE_03_When_the_audit_insert_fails_the_status_change_is_rolled_back()
    {
        var requester = await CreateUserAsync(Role.Requester);
        var request = await SubmitRequestAsync(requester);

        // Fault injection: a supervisor whose id is not in the users table. The engine allows the
        // Accept, but the AuditEntry insert breaks its foreign key, so the whole save must fail.
        var ghostSupervisor = new Actor(Guid.NewGuid(), new HashSet<Role> { Role.Supervisor });

        await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
            ApplyAsync(request.Id, 1, new LifecycleCommand(LifecycleEvent.Accept), ghostSupervisor));

        var saved = await LoadRequestAsync(request.Id);
        Assert.Equal(RequestStatus.Submitted, saved.Status);
        Assert.Equal(1, saved.Version);
        Assert.Equal(1, await CountAuditEntriesAsync(request.Id)); // only the original Submit
    }
}
