using CivicConnect.Infrastructure.Persistence;
using CivicConnect.Modules.Feedback;
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
        if (result.Outcome == TransitionOutcome.Forbidden)
            return new(LifecycleServiceOutcome.Forbidden, result.Message);

        // A stale page is a conflict even when the move is no longer legal (someone else already
        // accepted it): "reload and try again" is the useful answer, not "not allowed from Accepted".
        if (request.Version != expectedVersion)
            return new(LifecycleServiceOutcome.Conflict, ConflictMessage);

        if (!result.Succeeded)
            return new(Map(result.Outcome), result.Message);

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

        // FR-006: the notice is part of the same save, so it exists if and only if the change does
        if (FeedbackMessages.ForTransition(command.Event, request.Reference, audit.Detail) is string message)
        {
            db.FeedbackNotices.Add(new FeedbackNotice
            {
                RequestId = request.Id,
                RecipientId = request.RequesterId,
                Message = message,
                CreatedAt = now
            });
        }

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

    public async Task<LifecycleServiceResult> RecordActionAsync(
        Guid requestId, string? text, Actor actor, CancellationToken cancellationToken = default)
    {
        var request = await db.ServiceRequests.SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (request is null
            || !RequestVisibility.CanView(actor, new RequestAccessFacts(request.RequesterId, request.AssignedToId, request.Status)))
            return new(LifecycleServiceOutcome.NotFound, "Request not found.");

        // Authorise first, as for transitions, then validate
        var state = new RequestState(request.Status, request.AssignedToId, request.Version);
        if (!RequestActionRules.CanRecord(actor, state))
            return new(LifecycleServiceOutcome.Forbidden, "Only the assigned staff member or a supervisor can record actions while the work is open.");

        if (RequestActionRules.Validate(text) is string error)
            return new(LifecycleServiceOutcome.ValidationFailed, error);

        var now = time.GetUtcNow();
        var trimmed = text!.Trim();

        db.RequestActions.Add(new RequestAction
        {
            RequestId = request.Id,
            AuthorId = actor.UserId,
            Text = trimmed,
            CreatedAt = now
        });

        // FR-019: recording an action is a significant action, so it is audited; the status does not change
        db.AuditEntries.Add(new AuditEntry
        {
            RequestId = request.Id,
            ActorId = actor.UserId,
            EventType = "RecordAction",
            FromStatus = request.Status,
            ToStatus = request.Status,
            Detail = trimmed.Length > 200 ? trimmed[..199] + "…" : trimmed,
            OccurredAt = now
        });

        request.UpdatedAt = now;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The request's status changed while the action was being typed
            db.ChangeTracker.Clear();
            return new(LifecycleServiceOutcome.Conflict, ConflictMessage);
        }

        return new(LifecycleServiceOutcome.Applied, null, request.Version);
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
