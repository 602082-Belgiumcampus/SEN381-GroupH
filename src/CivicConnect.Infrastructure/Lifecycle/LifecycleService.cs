using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Modules.IdentityAccess;
using CivicConnect.Modules.Lifecycle;
using Microsoft.EntityFrameworkCore;

namespace CivicConnect.Infrastructure.Lifecycle;

/// <summary>
/// Request Lifecycle (FR-010, FR-011, FR-013, FR-019). Loads the request, lets the pure
/// <see cref="LifecycleEngine"/> decide, then saves the new state and its audit entry together.
/// Optimistic concurrency on ServiceRequest.Version stops a stale page overwriting a newer change.
/// </summary>
public sealed class LifecycleService(CivicConnectDbContext db, TimeProvider time) : ILifecycleService
{
    private const string ConflictMessage = "Someone else changed this request after you opened it. Reload it and try again.";

    public async Task<LifecycleServiceResult> ApplyAsync(
        Guid requestId,
        int expectedVersion,
        LifecycleCommand command,
        Actor actor,
        CancellationToken cancellationToken = default)
    {
        var request = await db.ServiceRequests.SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        // A request the actor may not see looks exactly like one that does not exist (ADR-007).
        if (request is null
            || !RequestVisibility.CanView(actor, new RequestAccessFacts(request.RequesterId, request.AssignedToId, request.Status)))
            return new(LifecycleServiceOutcome.NotFound, "Request not found.");

        var now = time.GetUtcNow();
        var current = new RequestState(
            request.Status, request.AssignedToId, request.Version,
            request.RejectionReason, request.ResolutionNote, request.ClosedAt);

        // Authorise, check the table and validate: all inside the pure engine, in that order.
        var result = LifecycleEngine.Apply(current, command, actor, now);
        if (!result.Succeeded)
            return new(Map(result.Outcome), result.Message);

        if (request.Version != expectedVersion)
            return new(LifecycleServiceOutcome.Conflict, ConflictMessage);

        if (command.Event == LifecycleEvent.Assign
            && !await IsActiveStaffMemberAsync(command.TargetAssigneeId!.Value, cancellationToken))
            return new(LifecycleServiceOutcome.ValidationFailed, "Choose an active staff member to assign.");

        var updated = result.State;
        request.Status = updated.Status;
        request.AssignedToId = updated.AssignedToId;
        request.Version = updated.Version;
        request.RejectionReason = updated.RejectionReason;
        request.ResolutionNote = updated.ResolutionNote;
        request.ClosedAt = updated.ClosedAt;
        request.UpdatedAt = now;

        var audit = result.Audit!;
        db.AuditEntries.Add(new AuditEntry
        {
            RequestId = request.Id,
            ActorId = audit.ActorId,
            EventType = audit.EventType,
            FromStatus = audit.From,
            ToStatus = audit.To,
            Detail = audit.Detail,
            OccurredAt = audit.OccurredAt
        });

        try
        {
            // The UPDATE only matches if Version is still the value we read; otherwise nothing is saved.
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return new(LifecycleServiceOutcome.Conflict, ConflictMessage);
        }

        return new(LifecycleServiceOutcome.Applied, null, updated.Version);
    }

    private Task<bool> IsActiveStaffMemberAsync(Guid userId, CancellationToken cancellationToken)
    {
        // A List (not an array): with C# 14 an array's Contains can bind to a span overload inside queries
        var staffRoleNames = new List<string> { nameof(Role.ServiceStaff), nameof(Role.Supervisor) };

        return (from user in db.Users
                join userRole in db.UserRoles on user.Id equals userRole.UserId
                join role in db.Roles on userRole.RoleId equals role.Id
                where user.Id == userId && user.IsActive && staffRoleNames.Contains(role.Name!)
                select user.Id)
            .AnyAsync(cancellationToken);
    }

    private static LifecycleServiceOutcome Map(TransitionOutcome outcome) => outcome switch
    {
        TransitionOutcome.Forbidden => LifecycleServiceOutcome.Forbidden,
        TransitionOutcome.InvalidTransition => LifecycleServiceOutcome.InvalidTransition,
        TransitionOutcome.ValidationFailed => LifecycleServiceOutcome.ValidationFailed,
        _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
    };
}
